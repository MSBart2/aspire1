using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Azure.Monitor.OpenTelemetry.AspNetCore;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";
    private const string LoggerCategoryName = "Microsoft.Extensions.Hosting.Extensions";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("aspire1.metrics"); // Custom application metrics
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Enable Azure Monitor (Application Insights) exporter when connection string is configured
        // For local development: use User Secrets or appsettings.Development.json
        // For Azure: automatically injected by azd or set via Key Vault reference
        var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (!string.IsNullOrEmpty(appInsightsConnectionString))
        {
            try
            {
                builder.Services.AddOpenTelemetry()
                   .UseAzureMonitor();
                builder.Logging.AddConsole().AddFilter((category, level) =>
                {
                    if (category == "Microsoft.Extensions.Hosting.Extensions" && level == LogLevel.Information)
                    {
                        return true;
                    }
                    return level >= LogLevel.Warning;
                });
                
                // Log Application Insights configuration using structured logging
                LogApplicationInsightsStatus(builder, "Application Insights telemetry enabled", LogLevel.Information);
            }
            catch (ArgumentException ex)
            {
                // Invalid connection string format
                LogApplicationInsightsStatus(builder, $"Invalid Application Insights configuration: {ex.Message}. Continuing in offline mode - telemetry will only go to OTLP/Dashboard", LogLevel.Warning, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Authentication/authorization failure
                LogApplicationInsightsStatus(builder, $"Application Insights authentication failed: {ex.Message}. Continuing in offline mode - telemetry will only go to OTLP/Dashboard", LogLevel.Warning, ex);
            }
            catch (Exception ex)
            {
                // Unexpected errors
                LogApplicationInsightsStatus(builder, $"Application Insights connection failed: {ex.Message}. Continuing in offline mode - telemetry will only go to OTLP/Dashboard", LogLevel.Warning, ex);
            }
        }
        else
        {
            LogApplicationInsightsStatus(builder, "Application Insights not configured (offline mode)", LogLevel.Information);
        }

        return builder;
    }

    private static void LogApplicationInsightsStatus<TBuilder>(TBuilder builder, string message, LogLevel logLevel, Exception? exception = null) where TBuilder : IHostApplicationBuilder
    {
        // Create a temporary logger factory to log during configuration phase
        // These messages are about Application Insights setup itself, so they use console logging
        // Once the app is running, Application Insights will capture all subsequent logs
        using var loggerFactory = LoggerFactory.Create(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration(builder.Configuration.GetSection("Logging"));
            loggingBuilder.AddConsole();
        });
        
        var logger = loggerFactory.CreateLogger(LoggerCategoryName);
        
        // Use the appropriate log method based on log level
        switch (logLevel)
        {
            case LogLevel.Warning:
                if (exception != null)
                    logger.LogWarning(exception, message);
                else
                    logger.LogWarning(message);
                break;
            case LogLevel.Error:
                if (exception != null)
                    logger.LogError(exception, message);
                else
                    logger.LogError(message);
                break;
            default:
                if (exception != null)
                    logger.LogInformation(exception, message);
                else
                    logger.LogInformation(message);
                break;
        }
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
