# Integration Test - Firebird with Testcontainers

This repository demonstrates how to create a test fixture using .NET Core and the Testcontainers NuGet package to run a Firebird database container and restore databases from backup files.

## Features

- ✅ .NET Core 10.0 xUnit test project
- ✅ Testcontainers for Docker container orchestration
- ✅ Firebird 4.0 database container support
- ✅ Database restore functionality from .fbk backup files
- ✅ Comprehensive test examples
- ✅ Automatic container lifecycle management

## Getting Started

Navigate to the `FirebirdIntegrationTests` directory for detailed documentation and usage examples.

```bash
cd FirebirdIntegrationTests
dotnet test
```

## Prerequisites

- .NET 10.0 SDK or later
- Docker Desktop (or Docker Engine)

## Project Structure

```
FirebirdIntegrationTests/
├── FirebirdTestFixture.cs       # Main test fixture class
├── FirebirdDatabaseTests.cs     # Sample database operation tests
├── FirebirdRestoreTests.cs      # Database restoration tests
├── README.md                    # Detailed documentation
└── FirebirdIntegrationTests.csproj
```

See [FirebirdIntegrationTests/README.md](FirebirdIntegrationTests/README.md) for detailed documentation.