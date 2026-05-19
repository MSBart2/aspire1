namespace aspire1.Contracts;

/// <summary>
/// Safe, developer-facing metadata describing where a weather response came from.
/// </summary>
/// <param name="CacheStatus">High-level cache status such as <c>hit</c> or <c>miss</c>.</param>
/// <param name="Source">Human-readable origin label for the forecast payload.</param>
/// <param name="RetrievedAtUtc">UTC timestamp for when the payload was assembled.</param>
/// <param name="MetricNames">Relevant metric names associated with the response, when discoverable.</param>
public sealed record WeatherDiagnostics(
    string CacheStatus,
    string Source,
    DateTimeOffset RetrievedAtUtc,
    string[] MetricNames);
