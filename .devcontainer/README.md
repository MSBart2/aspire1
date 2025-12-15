# .NET Aspire DevContainer

This devcontainer provides a complete development environment for the aspire1 solution with all necessary tools pre-configured.

## 🎯 Features

- **.NET 10.0 SDK** - Latest .NET runtime and SDK
- **.NET Aspire Workload** - Pre-installed Aspire tooling
- **Azure CLI** - With Container Apps extension
- **Azure Developer CLI (azd)** - For deployment automation
- **Docker-in-Docker** - For building and running containers
- **MinVer CLI** - Automatic SemVer versioning
- **GitHub CLI** - For GitHub Actions and repository management
- **Node.js LTS** - For frontend tooling if needed

## 💻 System Requirements

- **CPU:** 4 cores
- **Memory:** 8GB RAM
- **Storage:** 50GB minimum

## 🚀 Quick Start

### 1. Open in DevContainer

**VS Code:**

1. Install "Dev Containers" extension (`ms-vscode-remote.remote-containers`)
2. Open command palette (Ctrl+Shift+P / Cmd+Shift+P)
3. Select: **Dev Containers: Reopen in Container**

**GitHub Codespaces:**

1. Click "Code" → "Codespaces" → "Create codespace on main"
2. Container will automatically build and configure

### 2. Verify Setup

The post-create script runs automatically and will:

- ✅ Install MinVer CLI
- ✅ Restore NuGet packages
- ✅ Trust HTTPS development certificates
- ✅ Install Aspire workload
- ✅ Configure git for the container
- ✅ Install Git hooks (pre-commit and pre-push)

**Git Protections:**
Pre-commit and pre-push hooks are automatically installed to enforce branching strategy. Direct commits and pushes to `main`/`master` branches are blocked. Always use feature branches for development.

### 3. Start Developing

```bash
# Start Aspire dashboard
dotnet run --project aspire1.AppHost

# Access the dashboard
# HTTP:  http://localhost:15888
# HTTPS: https://localhost:18848
```

## 📡 Port Forwarding

The following ports are automatically forwarded:

| Port  | Service                  | Auto-Forward |
| ----- | ------------------------ | ------------ |
| 15888 | Aspire Dashboard (HTTP)  | Notify       |
| 18848 | Aspire Dashboard (HTTPS) | Notify       |
| 7002  | ApiService               | Silent       |
| 5188  | Web (Blazor)             | Silent       |

## 🔐 Azure Authentication

Your local Azure credentials are mounted into the container:

```bash
# Login to Azure (if needed)
az login

# Login to Azure Developer CLI
azd auth login

# Verify authentication
az account show
```

## 🛠️ Included VS Code Extensions

### .NET & Aspire

- C# Dev Kit (`ms-dotnettools.csdevkit`)
- C# (`ms-dotnettools.csharp`)
- .NET Runtime (`ms-dotnettools.vscode-dotnet-runtime`)
- Aspire (`microsoft-aspire.aspire-vscode`)

### Azure & Deployment

- Azure GitHub Copilot (`ms-azuretools.vscode-azure-github-copilot`)
- Azure MCP Server (`ms-azuretools.vscode-azure-mcp-server`)
- Azure Resource Groups (`ms-azuretools.vscode-azureresourcegroups`)
- Docker (`ms-azuretools.vscode-containers`)

### GitHub & Copilot

- GitHub Copilot (`github.copilot`)
- GitHub Copilot Chat (`github.copilot-chat`)
- GitHub Actions (`github.vscode-github-actions`)

### Productivity

- PowerShell (`ms-vscode.powershell`)
- Python (`ms-python.python`)
- Pylance (`ms-python.vscode-pylance`)
- ESLint (`dbaeumer.vscode-eslint`)
- Prettier (`esbenp.prettier-vscode`)
- Rainbow CSV (`mechatroner.rainbow-csv`)

## 🔧 Configuration

### VS Code Settings

The devcontainer automatically sets:

- Default solution: `aspire1.sln`
- Auto-start Aspire dashboard: `true`
- Exclude bin/obj from file watcher (performance)

### Environment Variables

- `ASPNETCORE_ENVIRONMENT=Development`
- `DOTNET_CLI_TELEMETRY_OPTOUT=1` (privacy)
- `DOTNET_NOLOGO=1` (cleaner output)

## 🎯 Common Tasks

### Build & Run

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run with Aspire dashboard
dotnet run --project aspire1.AppHost

# Run individual service
dotnet run --project aspire1.ApiService
dotnet run --project aspire1.Web
```

### Versioning

```bash
# Check current version
minver

# Tag a new version
git tag v1.1.0
git push --tags

# Build with version embedded
dotnet build
```

### Azure Deployment

```bash
# Login to Azure
azd auth login

# Initialize environment (first time only)
azd env new dev

# Deploy to Azure
azd up

# Check deployment status
azd monitor
```

### Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📚 Architecture Documentation

- [Solution Architecture](../ARCHITECTURE.md) - High-level topology
- [AppHost Configuration](../aspire1.AppHost/ARCHITECTURE.md) - Service orchestration
- [ApiService](../aspire1.ApiService/ARCHITECTURE.md) - REST API patterns (10 examples)
- [Web Service](../aspire1.Web/ARCHITECTURE.md) - Blazor patterns (12 examples)
- [Service Defaults](../aspire1.ServiceDefaults/ARCHITECTURE.md) - Shared configuration

## 🐛 Troubleshooting

### Dashboard Won't Start

```bash
# Check if ports are already in use
netstat -tuln | grep -E '15888|18848'

# Kill existing processes
pkill -f aspire1.AppHost
```

### HTTPS Certificate Issues

```bash
# Re-trust certificates
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Azure CLI Issues

```bash
# Re-login to Azure
az logout
az login

# Check subscription
az account show
az account list
```

### Git Issues

```bash
# Add workspace as safe directory
git config --global --add safe.directory /workspaces/aspire1

# Verify git status
git status

# Reinstall Git hooks (if needed)
cp scripts/hooks/* .git/hooks/
chmod +x .git/hooks/*
```

### Git Hooks Not Working

If pre-commit or pre-push hooks aren't preventing commits to main/master:

```bash
# Check if hooks are installed
ls -la .git/hooks/

# Make hooks executable
chmod +x .git/hooks/pre-commit
chmod +x .git/hooks/pre-push

# Test hook manually
.git/hooks/pre-commit
```

## 🔄 Updating the DevContainer

If you modify `.devcontainer/devcontainer.json`:

1. Open command palette (Ctrl+Shift+P)
2. Select: **Dev Containers: Rebuild Container**

## 🌐 GitHub Codespaces

This devcontainer works seamlessly with GitHub Codespaces:

- **2-core machine** - Basic development (slower builds)
- **4-core machine** - Recommended (matches hostRequirements)
- **8-core machine** - Fast builds and multiple services

**Cost optimization:**

- Codespaces auto-stop after 30 minutes of inactivity
- Use `azd down` before stopping to avoid Azure costs

## 📝 Notes

- **Mounted volumes:** Your local `~/.azure` and `~/.aspire` directories are mounted for credential persistence
- **Container name:** `aspire1-devcontainer` (useful for Docker commands)
- **Base image:** `mcr.microsoft.com/devcontainers/dotnet:1-10.0-noble` (Ubuntu 24.04 Noble with .NET 10)
- **Auto-updates:** Docker-in-Docker and Azure CLI extensions update automatically

## 🎉 Next Steps

1. Review [ARCHITECTURE.md](../ARCHITECTURE.md) for solution overview
2. Check [copilot-instructions.md](../.github/copilot-instructions.md) for Copilot guidance
3. Run `dotnet run --project aspire1.AppHost` to start coding!

---

**Happy coding! 🚀**
