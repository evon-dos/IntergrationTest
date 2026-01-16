using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FirebirdSql.Data.FirebirdClient;

namespace FirebirdIntegrationTests;

/// <summary>
/// Test fixture for running Firebird database in a Docker container using Testcontainers.
/// Supports database restoration from backup files.
/// </summary>
public class FirebirdTestFixture : IAsyncLifetime
{
    private const int FirebirdPort = 3050;
    private const string FirebirdUser = "SYSDBA";
    private const string FirebirdPassword = "masterkey";
    private const string DatabaseName = "test.fdb";
    
    private IContainer? _firebirdContainer;
    
    /// <summary>
    /// Gets the connection string for connecting to the Firebird database.
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;
    
    /// <summary>
    /// Gets the host port mapped to the Firebird container port.
    /// </summary>
    public int HostPort { get; private set; }

    /// <summary>
    /// Initializes the Firebird container asynchronously.
    /// </summary>
    public async Task InitializeAsync()
    {
        _firebirdContainer = new ContainerBuilder()
            .WithImage("jacobalberty/firebird:4.0")
            .WithPortBinding(FirebirdPort, true)
            .WithEnvironment("FIREBIRD_DATABASE", DatabaseName)
            .WithEnvironment("FIREBIRD_USER", FirebirdUser)
            .WithEnvironment("FIREBIRD_PASSWORD", FirebirdPassword)
            .WithEnvironment("ISC_PASSWORD", FirebirdPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(FirebirdPort))
            .Build();

        await _firebirdContainer.StartAsync();
        
        HostPort = _firebirdContainer.GetMappedPublicPort(FirebirdPort);
        
        ConnectionString = new FbConnectionStringBuilder
        {
            DataSource = "localhost",
            Port = HostPort,
            Database = DatabaseName,
            UserID = FirebirdUser,
            Password = FirebirdPassword,
            ServerType = FbServerType.Default
        }.ToString();
        
        // Wait a bit for the database to be fully initialized
        await Task.Delay(TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// Restores a database from a backup file.
    /// </summary>
    /// <param name="backupFilePath">Path to the backup file (.fbk)</param>
    /// <param name="databasePath">Optional database path inside the container (defaults to DatabaseName)</param>
    public async Task RestoreDatabaseFromBackupAsync(string backupFilePath, string? databasePath = null)
    {
        if (_firebirdContainer == null)
        {
            throw new InvalidOperationException("Container not initialized. Call InitializeAsync first.");
        }

        if (!File.Exists(backupFilePath))
        {
            throw new FileNotFoundException($"Backup file not found: {backupFilePath}");
        }

        databasePath ??= DatabaseName;
        
        // Copy backup file to container
        var backupFileName = Path.GetFileName(backupFilePath);
        var containerBackupPath = $"/tmp/{backupFileName}";
        
        await _firebirdContainer.CopyAsync(
            File.ReadAllBytes(backupFilePath),
            containerBackupPath);

        // Execute gbak restore command
        var restoreCommand = new[]
        {
            "/bin/bash",
            "-c",
            $"gbak -c -v {containerBackupPath} /firebird/data/{databasePath} -user {FirebirdUser} -password {FirebirdPassword}"
        };

        var execResult = await _firebirdContainer.ExecAsync(restoreCommand);
        
        if (execResult.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Database restore failed with exit code {execResult.ExitCode}. " +
                $"Stdout: {execResult.Stdout}, Stderr: {execResult.Stderr}");
        }
        
        // Update connection string to point to the restored database
        if (databasePath != DatabaseName)
        {
            ConnectionString = new FbConnectionStringBuilder
            {
                DataSource = "localhost",
                Port = HostPort,
                Database = databasePath,
                UserID = FirebirdUser,
                Password = FirebirdPassword,
                ServerType = FbServerType.Default
            }.ToString();
        }
        
        // Wait for database to be ready after restore
        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Creates a simple test database with a sample table.
    /// </summary>
    public async Task CreateSampleDatabaseAsync()
    {
        using var connection = new FbConnection(ConnectionString);
        await connection.OpenAsync();

        var createTableSql = @"
            CREATE TABLE TestTable (
                Id INTEGER NOT NULL PRIMARY KEY,
                Name VARCHAR(100),
                CreatedDate TIMESTAMP
            )";

        using var command = new FbCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();

        // Insert sample data
        var insertSql = "INSERT INTO TestTable (Id, Name, CreatedDate) VALUES (@Id, @Name, @CreatedDate)";
        using var insertCommand = new FbCommand(insertSql, connection);
        
        insertCommand.Parameters.Add("@Id", FbDbType.Integer);
        insertCommand.Parameters.Add("@Name", FbDbType.VarChar);
        insertCommand.Parameters.Add("@CreatedDate", FbDbType.TimeStamp);

        for (int i = 1; i <= 5; i++)
        {
            insertCommand.Parameters["@Id"].Value = i;
            insertCommand.Parameters["@Name"].Value = $"Test Item {i}";
            insertCommand.Parameters["@CreatedDate"].Value = DateTime.Now;
            await insertCommand.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Executes a SQL query and returns the result count.
    /// </summary>
    public async Task<int> ExecuteScalarAsync(string sql)
    {
        using var connection = new FbConnection(ConnectionString);
        await connection.OpenAsync();
        
        using var command = new FbCommand(sql, connection);
        var result = await command.ExecuteScalarAsync();
        
        return result != null ? Convert.ToInt32(result) : 0;
    }

    /// <summary>
    /// Disposes the Firebird container asynchronously.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_firebirdContainer != null)
        {
            await _firebirdContainer.DisposeAsync();
        }
    }
}
