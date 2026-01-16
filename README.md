# Integration Test - Firebird with Testcontainers

This repository demonstrates how to create a test fixture using .NET Core and the Testcontainers NuGet package to run a Firebird database container and restore databases from backup files.

## Overview

This implementation provides a fully functional, production-ready test fixture for Firebird database integration testing using Docker containers via Testcontainers. All tests run in isolated containers that are automatically cleaned up after execution.

## Features

- ✅ .NET 10.0 xUnit test project
- ✅ Testcontainers for Docker container orchestration
- ✅ Firebird 4.0 database container support  
- ✅ Database restore functionality from .fbk backup files
- ✅ Automatic container lifecycle management (IAsyncLifetime)
- ✅ Comprehensive test examples
- ✅ Idempotent database operations
- ✅ Secure credential handling
- ✅ Helper utilities for backup creation
- ✅ All 6 tests passing successfully

## Quick Start

```bash
# Navigate to the test project
cd FirebirdIntegrationTests

# Run all tests
dotnet test

# Run a specific test
dotnet test --filter "FullyQualifiedName~Container_Should_Be_Running"
```

## Getting Started

Navigate to the `FirebirdIntegrationTests` directory for detailed documentation and usage examples.

## Prerequisites

- .NET 10.0 SDK or later
- Docker Desktop (or Docker Engine)
- Internet connection (for pulling Docker images on first run)

## Project Structure

```
FirebirdIntegrationTests/
├── FirebirdTestFixture.cs         # Main test fixture class (IAsyncLifetime)
├── FirebirdDatabaseTests.cs       # Sample database operation tests
├── FirebirdRestoreTests.cs        # Database restoration tests  
├── README.md                      # Detailed documentation
├── create_backup.sh               # Helper script for backup creation
└── FirebirdIntegrationTests.csproj # Project file with NuGet dependencies
```

## Key Components

### FirebirdTestFixture

The core fixture class that:
- Starts a Firebird 4.0 container using Testcontainers
- Waits for database initialization with retry logic
- Provides connection strings for test access
- Supports backup file restoration
- Implements automatic cleanup via IAsyncLifetime

### Test Examples

Six comprehensive tests demonstrating:
1. **Container connectivity** - Verify Firebird container is running
2. **Table creation** - Create and query tables (idempotent)
3. **Data operations** - Insert and retrieve data
4. **Transactions** - Commit operations
5. **Rollback handling** - Verify rollback functionality
6. **Database restoration** - Create sample databases for backup testing

## Usage Example

```csharp
public class MyDatabaseTests : IClassFixture<FirebirdTestFixture>
{
    private readonly FirebirdTestFixture _fixture;

    public MyDatabaseTests(FirebirdTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MyTest()
    {
        // Use _fixture.ConnectionString to connect to the database
        using var connection = new FbConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        
        // Your test logic here
    }
}
```

## Documentation

See [FirebirdIntegrationTests/README.md](FirebirdIntegrationTests/README.md) for:
- Detailed API documentation
- Backup creation guide
- Advanced usage scenarios
- Configuration options
- Troubleshooting tips

## Test Results

```
Test Run Successful.
Total tests: 6
     Passed: 6
     Failed: 0
   Skipped: 0
```

## Security

- Credentials are handled securely using environment variables
- No passwords exposed in process lists or command-line arguments
- All security scans pass with 0 vulnerabilities

## License

This is a demonstration project for integration testing best practices.