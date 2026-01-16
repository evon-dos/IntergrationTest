# Firebird Integration Tests with Testcontainers

This project demonstrates how to use Testcontainers to run a Firebird database in Docker for integration testing with .NET Core.

## Features

- **Firebird Container Setup**: Automatically starts a Firebird database container using Testcontainers
- **Database Restoration**: Supports restoring databases from backup files (.fbk)
- **Test Fixture**: Reusable test fixture that manages container lifecycle
- **Sample Tests**: Comprehensive examples of database operations

## Prerequisites

- .NET 10.0 or later
- Docker (for running Testcontainers)

## NuGet Packages

The project uses the following NuGet packages:
- `Testcontainers` (3.10.0) - For container orchestration
- `FirebirdSql.Data.FirebirdClient` (10.3.1) - Firebird ADO.NET provider
- `xunit` - Testing framework

## Usage

### Basic Usage

The `FirebirdTestFixture` class manages the lifecycle of the Firebird container:

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

### Creating a Sample Database

```csharp
await _fixture.CreateSampleDatabaseAsync();
```

This creates a test table with sample data.

### Restoring from Backup

```csharp
var backupFilePath = "path/to/your/backup.fbk";
await _fixture.RestoreDatabaseFromBackupAsync(backupFilePath, "restored.fdb");
```

This restores a database from a Firebird backup file.

### Creating a Backup File

To create a backup file for testing:

1. Run your tests to start the container
2. Create sample data in the database
3. Use `gbak` command inside the container to create a backup:

```bash
docker exec -it <container_id> gbak -b /firebird/data/test.fdb /firebird/data/backup.fbk -user SYSDBA -password masterkey
```

4. Copy the backup file from the container:

```bash
docker cp <container_id>:/firebird/data/backup.fbk ./TestData/sample_backup.fbk
```

## Running Tests

```bash
dotnet test
```

## Test Structure

- **FirebirdTestFixture.cs**: Main fixture class that manages Firebird container
- **FirebirdDatabaseTests.cs**: Sample tests demonstrating database operations
- **FirebirdRestoreTests.cs**: Tests for database restoration functionality

## Key Methods

### FirebirdTestFixture

- `InitializeAsync()`: Starts the Firebird container
- `CreateSampleDatabaseAsync()`: Creates a test table with sample data
- `RestoreDatabaseFromBackupAsync(backupFilePath, databasePath)`: Restores database from backup
- `ExecuteScalarAsync(sql)`: Helper method to execute scalar queries
- `DisposeAsync()`: Cleans up the container

## Connection Details

The default configuration uses:
- **User**: SYSDBA
- **Password**: masterkey
- **Database**: test.fdb
- **Image**: jacobalberty/firebird:4.0

## Notes

- The container automatically starts on a random available port
- Database is created automatically on container startup
- Container cleanup happens automatically after tests complete
- Wait strategies ensure the database is ready before tests run
