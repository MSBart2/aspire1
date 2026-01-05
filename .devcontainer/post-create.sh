#!/bin/bash
set -e

echo "🚀 Setting up .NET Aspire development environment..."

# Pre-install .NET runtimes needed by VS Code extensions
# This prevents downloads on every container start
echo "📦 Pre-installing .NET runtimes for VS Code extensions..."
export DOTNET_ROOT=/usr/share/dotnet
export PATH="$DOTNET_ROOT:$PATH"

# Install .NET 9 ASP.NET Core runtime (needed by C# DevKit)
if ! dotnet --list-runtimes | grep -q "Microsoft.AspNetCore.App 9.0"; then
    echo "  Installing .NET 9 ASP.NET Core runtime..."
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --runtime aspnetcore --version 9.0 --install-dir $DOTNET_ROOT
    rm dotnet-install.sh
fi

# Install MinVer CLI for versioning
echo "📦 Installing MinVer CLI..."
dotnet tool install --global minver-cli || dotnet tool update --global minver-cli

# Restore .NET solution
echo "📦 Restoring .NET solution..."
dotnet restore aspire1.sln

# Trust development certificates
echo "🔐 Trusting HTTPS development certificates..."
dotnet dev-certs https --trust || true

# Install Aspire workload (if not already installed)
echo "📦 Installing .NET Aspire workload..."
dotnet workload update
dotnet workload install aspire || echo "Aspire workload already installed"

# Set git config for container
echo "🔧 Configuring git..."
git config --global --add safe.directory /workspaces/aspire1 || true
git config --global init.defaultBranch main || true

# Install Git hooks
echo "🪝 Installing Git hooks..."
if [ -d "scripts/hooks" ]; then
    mkdir -p .git/hooks
    cp scripts/hooks/* .git/hooks/
    chmod +x .git/hooks/*
    echo "  ✅ Installed pre-commit and pre-push hooks"
else
    echo "  ⚠️  Warning: scripts/hooks directory not found"
fi

# Create local secrets directory
echo "🔐 Setting up user secrets..."
mkdir -p ~/.microsoft/usersecrets

# Display version info
echo ""
echo "✅ Development environment ready!"
echo ""
echo "📊 Installed versions:"
dotnet --version
echo "Azure CLI: $(az version -o tsv 2>/dev/null || echo 'Not installed')"
echo "Azure Developer CLI: $(azd version 2>/dev/null || echo 'Not installed')"
echo "MinVer: $(minver --version 2>/dev/null || echo 'Not installed')"
echo ""
echo "🔒 Git Protections:"
echo "  • Pre-commit and pre-push hooks installed"
echo "  • Direct commits/pushes to main/master are blocked"
echo "  • Use feature branches for all development"
echo ""
echo "🎯 Quick start commands:"
echo "  dotnet run --project aspire1.AppHost      # Start Aspire dashboard"
echo "  azd auth login                            # Login to Azure"
echo "  azd up                                    # Deploy to Azure"
echo ""
echo "📚 Dashboard will be available at:"
echo "  HTTP:  http://localhost:15888"
echo "  HTTPS: https://localhost:18848"
echo ""
