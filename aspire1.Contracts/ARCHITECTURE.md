# Architecture - aspire1.Contracts

> **Component Type:** Class Library (Shared DTO)
> **Framework:** .NET 10
> **Purpose:** Single source of truth for shared data transfer objects (DTOs) used across API and frontend projects

## 🎯 Overview

`aspire1.Contracts` is a minimal class library containing shared DTO types referenced by both `aspire1.WeatherService` and `aspire1.Web`. It has no dependencies on ASP.NET Core, Blazor, or any Azure SDK — it is pure data types only.

**Why it exists:** Without a shared contract, duplicate record definitions can silently diverge. Adding a field to the API without updating the client causes silent data loss during JSON deserialization. A shared project makes any schema change a compile-time error in both consumers.

## 🏗️ Architecture

```
aspire1.Contracts
└── WeatherForecast.cs      ← Single canonical DTO definition

Referenced by:
├── aspire1.WeatherService  ← Serializes WeatherForecast[] as API response
└── aspire1.Web             ← Deserializes WeatherForecast[] from API
```

Both service projects reference `aspire1.Contracts` via `<ProjectReference>`. Test projects pick up the type transitively through their parent service references — no direct reference needed.

## 📦 DTOs

### WeatherForecast

```csharp
namespace aspire1.Contracts;

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary, int Humidity)
{
    public int TemperatureF => (int)Math.Round(TemperatureC * 1.8 + 32);
}
```

| Property | Type | Notes |
|---|---|---|
| `Date` | `DateOnly` | Forecast date |
| `TemperatureC` | `int` | Temperature in Celsius |
| `TemperatureF` | `int` (computed) | Converted using `Math.Round` for accuracy |
| `Summary` | `string?` | Textual description (nullable) |
| `Humidity` | `int` | Percentage; controlled by `WeatherHumidity` feature flag |

> **TemperatureF formula:** Uses `Math.Round(TemperatureC * 1.8 + 32)` — the fix from issue #24. The legacy `32 + (int)(TemperatureC / 0.5556)` formula had precision errors and is retired.

## ✅ Best Practices vs Anti-Patterns

### ❌ BAD: Duplicate local record definitions

```csharp
// In aspire1.WeatherService/Services/CachedWeatherService.cs
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary, int Humidity)
{
    public int TemperatureF => (int)Math.Round(TemperatureC * 1.8 + 32);
}

// ALSO in aspire1.Web/WeatherApiClient.cs — identical today, diverges silently tomorrow
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary, int Humidity)
{
    public int TemperatureF => (int)Math.Round(TemperatureC * 1.8 + 32);
}
```

**Why it's bad:** No compile-time enforcement. A field added to one definition and not the other causes silent deserialization data loss at runtime.

### ✅ GOOD: Single shared definition in aspire1.Contracts

```csharp
// aspire1.Contracts/WeatherForecast.cs — one definition, two consumers
namespace aspire1.Contracts;

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary, int Humidity)
{
    public int TemperatureF => (int)Math.Round(TemperatureC * 1.8 + 32);
}
```

**Why it's good:** Any field change is a compile-time error in both `aspire1.WeatherService` and `aspire1.Web`. Schema drift is impossible without a build failure.

## 🔒 Design Constraints

- **No HTTP dependencies** — this is a pure DTO library. Never add `Microsoft.AspNetCore.*` packages here.
- **No Azure SDK dependencies** — no Azure, Redis, or App Configuration references.
- **No business logic** — computed properties on records (like `TemperatureF`) are acceptable; service methods are not.
- **`net10.0` target** — matches both consumer projects; `Directory.Build.props` handles MinVer versioning automatically.
