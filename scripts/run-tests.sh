#!/bin/bash

# Playwright Tests Setup and Runner for aspire1
# This script helps set up and run Playwright tests for the .NET Aspire application

set -e

echo "🎭 aspire1 Playwright Test Runner"
echo "=================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

# Check if running in GitHub Codespaces
if [ -n "$CODESPACES" ]; then
    print_status "Running in GitHub Codespaces environment"
    CODESPACE_NAME=$CODESPACE_NAME

    # Construct the base URL for Codespaces
    if [ -n "$CODESPACE_NAME" ]; then
        BASE_URL="https://$CODESPACE_NAME.app.github.dev"
        print_status "Detected Codespace URL: $BASE_URL"
        export PLAYWRIGHT_BASE_URL="$BASE_URL"
    else
        print_warning "Could not detect Codespace name. You may need to set PLAYWRIGHT_BASE_URL manually."
    fi
else
    print_status "Running in local environment"
    export PLAYWRIGHT_BASE_URL="http://localhost:5000"
fi

# Check if .NET Aspire application is running
print_status "Checking if aspire1 application is running..."

if [ -n "$PLAYWRIGHT_BASE_URL" ]; then
    # Try to check if the application is accessible
    if curl -s --max-time 5 "$PLAYWRIGHT_BASE_URL" >/dev/null 2>&1; then
        print_success "Application appears to be running at $PLAYWRIGHT_BASE_URL"
    else
        print_warning "Application may not be running or accessible at $PLAYWRIGHT_BASE_URL"

        if [ -z "$CODESPACES" ]; then
            print_status "Attempting to start the application..."
            # Start the application in the background
            dotnet run --project aspire1.AppHost &
            APP_PID=$!
            print_status "Started application with PID: $APP_PID"

            # Wait for the application to be ready
            print_status "Waiting for application to be ready..."
            for i in {1..30}; do
                if curl -s --max-time 2 "http://localhost:5000" >/dev/null 2>&1; then
                    print_success "Application is now running!"
                    break
                fi
                echo -n "."
                sleep 2
            done
            echo
        else
            print_error "Please ensure the aspire1 application is running before executing tests."
            echo "Run: dotnet run --project aspire1.AppHost"
            exit 1
        fi
    fi
fi

# Install Playwright dependencies if needed
print_status "Checking Playwright installation..."
if ! npm list @playwright/test >/dev/null 2>&1; then
    print_status "Installing Playwright test framework..."
    npm install
fi

# Install Playwright browsers if needed
if ! npx playwright --version >/dev/null 2>&1; then
    print_error "Playwright is not properly installed"
    exit 1
fi

# Check for browser installation
print_status "Checking Playwright browsers..."
if ! ls ~/.cache/ms-playwright/chromium-* >/dev/null 2>&1; then
    print_status "Installing Playwright browsers..."
    npx playwright install

    # Install system dependencies on Linux
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        print_status "Installing system dependencies..."
        npx playwright install-deps || {
            print_warning "Could not install system dependencies automatically."
            print_warning "You may need to run: sudo npx playwright install-deps"
        }
    fi
fi

# Determine test suite to run
TEST_SUITE=${1:-"all"}

case "$TEST_SUITE" in
    "all")
        print_status "Running all test suites..."
        npm test
        ;;
    "api")
        print_status "Running Weather API tests..."
        npm run test:api
        ;;
    "web")
        print_status "Running Web application tests..."
        npm run test:web
        ;;
    "integration")
        print_status "Running Integration tests..."
        npm run test:integration
        ;;
    "performance")
        print_status "Running Performance tests..."
        npm run test:performance
        ;;
    "headed")
        print_status "Running tests with browser UI..."
        npm run test:headed
        ;;
    "debug")
        print_status "Running tests in debug mode..."
        npm run test:debug
        ;;
    *)
        print_error "Unknown test suite: $TEST_SUITE"
        echo "Available options: all, api, web, integration, performance, headed, debug"
        exit 1
        ;;
esac

# Show test results
if [ $? -eq 0 ]; then
    print_success "Tests completed successfully!"
    print_status "View detailed results: npm run test:report"
else
    print_error "Some tests failed. Check the output above for details."
    exit 1
fi

# Cleanup
if [ -n "$APP_PID" ]; then
    print_status "Stopping application (PID: $APP_PID)..."
    kill $APP_PID 2>/dev/null || true
fi

print_success "Test execution complete!"