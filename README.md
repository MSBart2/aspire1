# 🚀 aspire1 - Your Cloud-Native Playground

> **Because deploying to Azure should be this easy.** ✨

Welcome to **aspire1**, a production-ready .NET Aspire application that's basically showing off. It's got everything: Blazor Server, Minimal APIs, Redis caching, Azure App Configuration, Feature Flags, and—oh yeah—**Application Insights with custom metrics that'll make your dashboards jealous**.

Think of it as the Swiss Army knife of modern .NET applications, except it actually deploys to Azure Container Apps without making you cry. 🎉

---

## 🎯 What's This Thing Do?

**Short answer:** Weather forecasts. Revolutionary, right?

**Real answer:** This is a **reference architecture** that demonstrates how to build cloud-native applications with .NET Aspire. It's got all the bells and whistles you'd want in a production app:

- 🌐 **Blazor Server** frontend (because SignalR is cool)
- 🎨 **Beautiful card-based UI** (with smooth hover effects and responsive layout)
- 🔌 **REST API** backend (Minimal APIs style, naturally)
- 💧 **Humidity tracking** (with feature flag control)
- 📊 **Custom telemetry** (tracks sunny days and counter clicks—priorities!)
- 💾 **Redis caching** (with graceful offline fallback)
- 🎚️ **Feature flags** (toggle features without redeploying like a boss)
- 🔐 **Key Vault secrets** (because committing passwords is _so_ 2015)
- 📈 **Application Insights** (with pre-built dashboards that actually make sense)
- 🚨 **Automated alerts** (emails you when things get spicy)

---

## 🎬 Quick Start (The "Just Let Me Run It" Version)

### Prerequisites

- **.NET 10.0 SDK** ([download here](https://dotnet.microsoft.com/download))
- **Docker Desktop** (optional for local dev, but Redis appreciates it)
- **Azure CLI** ([install here](https://aka.ms/azure-cli))
- **azd** ([Azure Developer CLI](https://aka.ms/azd-install))
- A sense of adventure 🧭

### Run Locally (Offline-First FTW)

```bash
# Clone this bad boy
git clone https://github.com/rbmathis/aspire1.git
cd aspire1

# Restore packages (grab a coffee ☕)
dotnet restore

# Fire up the engines
dotnet run --project aspire1.AppHost/aspire1.AppHost.csproj
```

**Boom!** 💥 Your app is now running at:

- 🎛️ **Aspire Dashboard**: https://localhost:15888 (where the magic happens)
- 🌐 **Web Frontend**: https://localhost:7296 (click things, break things)
- 🔌 **API**: https://localhost:7002 (JSON for days)

**Pro tip:** No Azure? No problem! The app runs perfectly offline with in-memory fallbacks. Click the counter, check the weather, watch the metrics fly. 📊

---

## ☁️ Deploy to Azure

### Quick Deploy (Single Environment)

```bash
# Login to Azure (just once)
azd auth login

# Deploy EVERYTHING with one command
azd up
```

That's it. Seriously. `azd` will:

- ✅ Provision Azure resources (Container Apps, App Insights, Key Vault, Redis, App Config)
- ✅ Build Docker images
- ✅ Push to Azure Container Registry
- ✅ Deploy to Azure Container Apps
- ✅ Set up custom dashboards and alerts
- ✅ Pour you a virtual champagne 🍾

**Time:** ~3-5 minutes (depending on how fast Azure feels today)

### Production Pipeline (3 Environments: Dev → Stage → Prod)

The repository includes a **multistage CI/CD pipeline** with:

- ✅ **Parallel testing** (Web + API tests run simultaneously)
- ✅ **3 environments** with separate subscriptions/approvals
- ✅ **Automatic dev deployments** on push to main
- ✅ **Manual approvals** for stage and prod
- ✅ **OIDC authentication** (no secrets stored!)
- ✅ **Health checks** and deployment verification

**Deployment Flow:**

```
main branch → Auto-deploy to Dev (~6-10 min)
Tag v* → Dev → Stage (approval) → Prod (approval) (~15-20 min)
```

**Setup:** See [`.github/workflows/PIPELINE_SETUP.md`](.github/workflows/PIPELINE_SETUP.md) for complete setup instructions including:

- Azure service principal configuration with OIDC
- GitHub environment and secrets setup
- Usage examples and troubleshooting

**Quick Start Pipeline:**

```bash
# For dev deployment (automatic on main)
git add .
git commit -m "Add new feature"
git push origin main
# → Automatically deploys to dev after tests pass

# For full release (dev → stage → prod)
git tag v1.2.3
git push origin v1.2.3
# → Deploys to dev, waits for approvals for stage and prod
```

---

## 🏗️ Architecture (The Visual Learner Special)

```
┌─────────────┐
│   Browser   │
│   👤 User   │
└──────┬──────┘
       │
       ▼
┌─────────────────────┐
│  Azure Front Door   │  ← "The Bouncer"
└──────────┬──────────┘
           │
           ▼
    ┌──────────────────────────┐
    │  Azure Container Apps    │
    │  Environment             │
    ├──────────────────────────┤
    │                          │
    │  ┌────────────────────┐  │
    │  │  aspire1-web       │  │ ← Blazor Server (the pretty one)
    │  │  Blazor Server     │  │
    │  └──────┬─────────────┘  │
    │         │                │
    │         │ service        │
    │         │ discovery      │
    │         ▼                │
    │  ┌────────────────────┐  │
    │  │  aspire1-weatherservice│  │ ← REST API (the smart one)
    │  │  Minimal API       │  │
    │  └────────────────────┘  │
    │                          │
    └──────────────────────────┘
               │
               │ telemetry
               ▼
    ┌────────────────────┐
    │  App Insights      │  ← "The Snitch" (in a good way)
    │  + Custom Metrics  │
    │  + Alerts          │
    │  + Dashboards      │
    └────────────────────┘
```

**Translation:** Browser talks to Blazor, Blazor talks to API, everything snitches to Application Insights. 🕵️

---

## 📊 Custom Metrics (Because Default Telemetry is Boring)

This app tracks **6 custom metrics** that actually matter:

| Metric                               | What It Does                   | Why You Care                             |
| ------------------------------------ | ------------------------------ | ---------------------------------------- |
| 🖱️ **counter.clicks**                | Counts button clicks by range  | See how bored your users are             |
| 🌤️ **weather.sunny.count**           | Tracks sunny forecasts by temp | Plan your beach day (or server capacity) |
| 📞 **weather.api.calls**             | Total API call volume          | Detect traffic spikes before they bite   |
| 💾 **cache.hits** / **cache.misses** | Cache performance              | Optimize that Redis bill                 |
| ⏱️ **api.call.duration**             | API latency distribution       | Keep users from rage-quitting            |

**View them:**

- **Locally:** Aspire Dashboard → Metrics → `aspire1.metrics`
- **Azure:** Application Insights → Custom Metrics

**Query them:**

```kusto
// Find your cache hit rate (higher = 💰 saved)
customMetrics
| where name == "cache.hits" or name == "cache.misses"
| summarize Hits = sumif(value, name == "cache.hits"),
            Misses = sumif(value, name == "cache.misses")
| extend HitRate = round(Hits * 100.0 / (Hits + Misses), 2)
```

---

## 🚨 Automated Alerts (The "Wake Up at 3 AM" Feature)

Three alerts are configured out-of-the-box:

1. **Cache Miss Rate >50%**

   - _Translation:_ "Your cache is basically decorative at this point"
   - **Severity:** ⚠️ Warning

2. **API Errors >5/min**

   - _Translation:_ "Houston, we have a problem"
   - **Severity:** 🚨 Error

3. **API Latency P95 >1000ms**
   - _Translation:_ "Users are watching their life flash before their eyes"
   - **Severity:** ⚠️ Warning

**Configure alert email:**

```bash
azd env set ALERT_EMAIL "your-email@example.com"
azd up
```

---

## 🎮 Cool Features to Try

### 1. **The Counter That Tells on You**

Navigate to `/counter` and start clicking. Watch the metrics dashboard light up like a Christmas tree. Each click is tracked with range categorization (0-10, 11-50, 51-100, 100+).

**Why ranges?** Turns out tracking individual numbers 1-10000 gets expensive. Grouping = smarter metrics = lower Azure bill. 💰

### 2. **Weather Forecasts with Beautiful Cards**

Hit `/weather` and enjoy the redesigned card-based UI! Each day's forecast is displayed in a beautiful, responsive card with:

- 📅 **Date Display** - Clear, readable date format
- 🌡️ **Temperature** - Both Celsius and Fahrenheit
- ☀️ **Weather Summary** - Descriptive conditions
- 💧 **Humidity** - Real-time humidity percentage (controlled by feature flag)

The cards feature smooth hover effects and automatically adjust to your screen size (3 columns on desktop, 2 on tablet, 1 on mobile).

**Feature Flag Control:** The humidity display is controlled by the `WeatherHumidity` feature flag—toggle it on/off without redeploying!

The API tracks:

- How many times you called it (mild stalking)
- How many "Sunny" days appear (optimism metrics)
- Temperature ranges (for the data nerds)
- Humidity levels (weather nerd approved 🌦️)

### 3. **Feature Flags Magic**

Toggle features on/off in Azure App Configuration **without redeploying**:

```bash
# Disable weather feature (chaos mode)
az appconfig kv set --name <your-appconfig> \
  --key ".appconfig.featureflag/WeatherForecast" \
  --value '{"enabled":false}'

# Disable humidity display (mystery mode)
az appconfig kv set --name <your-appconfig> \
  --key ".appconfig.featureflag/WeatherHumidity" \
  --value '{"enabled":false}'

# Watch the changes take effect within 30 seconds 🔥
```

**Available Feature Flags:**

- `WeatherForecast` - Controls entire weather API availability
- `DetailedHealth` - Controls health endpoint detail level
- `WeatherHumidity` - Controls humidity data display in the UI

### 4. **Cache Performance Theater**

The weather API uses Redis caching with a 5-minute TTL. Watch the cache metrics:

- First call: Cache MISS (generates data)
- Next ~10 calls: Cache HIT (blazing fast 🏎️)
- After 5 minutes: Cache MISS again (the circle of life)

---

## 🗂️ Project Structure (The Guided Tour)

```
aspire1/
├── aspire1.AppHost/           # 🎛️ The Orchestra Conductor
│   ├── AppHost.cs             # Service topology & discovery magic
│   └── ARCHITECTURE.md        # Deep dive docs
│
├── aspire1.WeatherService/        # 🔌 The Backend Ninja
│   ├── Program.cs             # Minimal APIs + custom metrics
│   ├── Services/
│   │   └── CachedWeatherService.cs  # Redis caching genius
│   └── ARCHITECTURE.md        # API endpoint docs
│
├── aspire1.Web/               # 🌐 The Pretty Face
│   ├── Program.cs             # Blazor Server config
│   ├── Components/Pages/      # Razor components
│   ├── WeatherApiClient.cs    # Typed HTTP client
│   └── ARCHITECTURE.md        # Frontend architecture
│
├── aspire1.ServiceDefaults/   # ⚙️ The Shared Brain
│   ├── Extensions.cs          # OpenTelemetry + health checks
│   ├── ApplicationMetrics.cs  # Custom metrics instruments
│   └── ARCHITECTURE.md        # Observability patterns
│
├── infra/                     # ☁️ Infrastructure as Code
│   ├── main.bicep             # Main orchestration
│   ├── app-insights.bicep     # Telemetry resources
│   ├── dashboard.bicep        # Pre-built visualizations
│   └── alerts.bicep           # Automated alerts
│
├── ARCHITECTURE.md            # 📖 High-level architecture
├── TELEMETRY.md               # 📊 Telemetry deep dive
└── README.md                  # 👋 You are here
```

**Pro tip:** Each project has its own `ARCHITECTURE.md` with deep technical details. Read them when coffee kicks in. ☕

---

## 🛠️ Development Workflow

### Local Development (The Happy Path)

```bash
# Start everything
dotnet run --project aspire1.AppHost

# Make changes to code
# Press Ctrl+R to reload (hot reload FTW)

# Check metrics at https://localhost:15888
# Watch logs in real-time
# Feel like a 10x developer 😎
```

### Testing (Unit + Integration + E2E)

**Unit & Integration Tests** (.NET):

```bash
# Run all tests
dotnet test

# Run specific project tests
dotnet test aspire1.WeatherService.Tests
dotnet test aspire1.Web.Tests

# With coverage metrics
dotnet test /p:CollectCoverage=true
```

**End-to-End Tests** (Playwright):

```bash
# Start the app first (or let Playwright auto-start)
dotnet run --project aspire1.AppHost

# Run all E2E tests
npm test

# Run specific test suites
npm run test:api          # Weather API endpoints only
npm run test:web          # Blazor UI only
npm run test:integration  # Full service communication
npm run test:performance  # Load & cache performance

# Run tests in TRACE MODE to capture screenshots and videos
npx playwright test --trace=on

# View the trace report with screenshots/videos
npx playwright show-trace trace/trace.zip

# View detailed HTML report (includes test results summary)
npm run test:report

# Debug mode (show browser UI)
npm run test:headed

# Step-through debugging
npm run test:debug
```

**Trace Mode Deep Dive** 🎬

Playwright's trace mode captures **everything**—screenshots, videos, network logs, DOM snapshots. Perfect for debugging failed tests or analyzing test behavior:

```bash
# Run tests with trace recording (creates trace/trace.zip)
npx playwright test --trace=on

# Open the trace viewer (interactive UI with playback)
npx playwright show-trace trace/trace.zip

# Inside the viewer:
# - Step through each action frame-by-frame
# - Click on any action to see the screenshot
# - Inspect network requests and responses
# - View console logs and errors
# - Compare expected vs actual DOM state
```

**Pro tip:** Traces are saved to `trace/trace.zip`. Use `show-trace` command immediately after test run for fastest debugging. Screenshots appear for every action (click, hover, navigate, etc.), making it easy to spot exactly where tests fail. 📸

**What Gets Tested:**

- 🌐 **API Contracts** - REST endpoints, response formats, status codes
- 🎨 **UI Interactions** - Navigation, forms, Counter page, Weather cards
- 🔗 **Service Communication** - Web ↔ API, service discovery validation
- 💾 **Caching** - Redis hit/miss scenarios, performance improvement
- 📊 **Performance** - Load times, concurrent user simulation, bundle sizes
- 📈 **Metrics** - Custom telemetry generation and validation

### Versioning (Automatic SemVer)

This project uses **MinVer** for automatic semantic versioning based on git tags:

```bash
# Check current version
minver

# Tag a new release
git tag -a v1.2.0 -m "Added sunny forecast tracking"
git push --tags

# Next build will be v1.2.0
```

---

## 🔐 Secrets Management (The Paranoid Edition)

### Local Development: User Secrets

```bash
# Set secrets for local dev (never commits to git)
dotnet user-secrets set "ConnectionStrings:MyDb" "..." --project aspire1.WeatherService
```

### Azure: Key Vault References

```bash
# In Azure, connection strings are injected via Key Vault
# Format: @Microsoft.KeyVault(SecretUri=https://...)
# Managed Identity handles auth (zero passwords in code)
```

**Golden Rule:** If it's a password, API key, or connection string, it goes in Key Vault. No exceptions. 🔒

---

## 🐛 Troubleshooting (When Things Get Weird)

### "My metrics aren't showing up!"

```bash
# Check Aspire Dashboard
# Navigate to https://localhost:15888 → Metrics
# Search for "aspire1.metrics"

# If empty, generate data:
# - Click counter button 50 times
# - Visit /weather page
# - Check metrics again
```

### "Application Insights isn't receiving data"

```bash
# Verify connection string
azd env get-values | grep APPLICATIONINSIGHTS_CONNECTION_STRING

# Check console logs for:
# ✅ Application Insights telemetry enabled
# (If you see ⚠️ offline mode, connection string is missing)
```

### "Build failed with restore errors"

```bash
# Nuclear option (fixes 99% of weird build issues)
dotnet clean
rm -rf **/bin **/obj
dotnet restore
dotnet build
```

### "I deployed but nothing works"

```bash
# Check deployment logs
azd deploy --debug

# View container logs
az containerapp logs show --name aspire1-web --resource-group <rg-name> --follow

# Common issues:
# - Missing environment variables
# - Key Vault permissions not set
# - App Configuration not configured
```

---

## 📚 Learn More (The Rabbit Hole)

### Official Docs

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)
- [Application Insights](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)

### In This Repo

- [`ARCHITECTURE.md`](ARCHITECTURE.md) - High-level solution architecture
- [`TELEMETRY.md`](TELEMETRY.md) - Custom metrics deep dive
- [`aspire1.ServiceDefaults/ARCHITECTURE.md`](aspire1.ServiceDefaults/ARCHITECTURE.md) - OpenTelemetry patterns
- [`aspire1.WeatherService/ARCHITECTURE.md`](aspire1.WeatherService/ARCHITECTURE.md) - API design
- [`aspire1.Web/ARCHITECTURE.md`](aspire1.Web/ARCHITECTURE.md) - Blazor Server architecture
- [`aspire1.AppHost/ARCHITECTURE.md`](aspire1.AppHost/ARCHITECTURE.md) - Service orchestration

---

## 🤝 Contributing

Found a bug? Want to add a feature? Have a better way to track sunny days?

**Pull requests welcome!** Just:

1. Fork it 🍴
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request 🎉

**Branching Strategy:** We use feature branches. No direct commits to `main` (Git hooks will yell at you).

---

## 📝 License

This project is licensed under the **"Do Whatever You Want But Don't Blame Me"** License.

(Okay, fine, it's MIT. Use it, abuse it, learn from it. Just don't sue us if your production app tracks too many sunny days.)

---

## 🎉 Credits

Built with:

- ☕ **Lots of coffee**
- 🎵 **Good music**
- 💻 **.NET 10.0** (preview but production-ready-ish)
- ☁️ **Azure Container Apps** (surprisingly easy)
- 📊 **OpenTelemetry** (because observability is cool now)
- ❤️ **A slight obsession with metrics**

---

## 💬 Questions?

**"Why weather forecasts?"**
Because everyone needs weather, even if it's fake. Plus, it's just complex enough to demonstrate real-world patterns without getting boring.

**"Is this production-ready?"**
Yes! All patterns here are production-grade. We're using it ourselves. The weather data is fake, but the architecture is real.

**"Can I use this for my startup?"**
Absolutely! Fork it, rename it, make it yours. Just remember us when you're a unicorn 🦄.

**"What's with all the emojis?"**
We believe documentation should be fun. Sue us. (Please don't actually sue us.)

---

<div align="center">

## 🌟 Star This Repo If You Found It Useful! 🌟

Made with 💙 by developers who actually read documentation

**Now go build something awesome.** 🚀

</div>
