using FirebirdSql.Data.FirebirdClient;

namespace FirebirdIntegrationTests;

/// <summary>
/// Integration tests demonstrating the FirebirdTestFixture usage.
/// </summary>
public class FirebirdDatabaseTests : IClassFixture<FirebirdTestFixture>
{
    private readonly FirebirdTestFixture _fixture;

    public FirebirdDatabaseTests(FirebirdTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Container_Should_Be_Running()
    {
        // Arrange & Act
        using var connection = new FbConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Assert
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }

    [Fact]
    public async Task Should_Create_And_Query_Table()
    {
        // Arrange
        await _fixture.CreateSampleDatabaseAsync();

        // Act
        var count = await _fixture.ExecuteScalarAsync("SELECT COUNT(*) FROM TestTable");

        // Assert
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task Should_Insert_And_Retrieve_Data()
    {
        // Arrange
        await _fixture.CreateSampleDatabaseAsync();

        // Act
        using var connection = new FbConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var selectSql = "SELECT Name FROM TestTable WHERE Id = @Id";
        using var command = new FbCommand(selectSql, connection);
        command.Parameters.Add("@Id", FbDbType.Integer).Value = 1;
        
        var name = await command.ExecuteScalarAsync();

        // Assert
        Assert.NotNull(name);
        Assert.Equal("Test Item 1", name.ToString());
    }

    [Fact]
    public async Task Should_Execute_Transactions()
    {
        // Arrange
        await _fixture.CreateSampleDatabaseAsync();

        using var connection = new FbConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Act
        using var transaction = await connection.BeginTransactionAsync();
        
        var insertSql = "INSERT INTO TestTable (Id, Name, CreatedDate) VALUES (@Id, @Name, @CreatedDate)";
        using var command = new FbCommand(insertSql, connection, transaction);
        
        command.Parameters.Add("@Id", FbDbType.Integer).Value = 100;
        command.Parameters.Add("@Name", FbDbType.VarChar).Value = "Transaction Test";
        command.Parameters.Add("@CreatedDate", FbDbType.TimeStamp).Value = DateTime.Now;
        
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();

        // Assert
        var count = await _fixture.ExecuteScalarAsync("SELECT COUNT(*) FROM TestTable WHERE Id = 100");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Should_Handle_Rollback()
    {
        // Arrange
        await _fixture.CreateSampleDatabaseAsync();
        var initialCount = await _fixture.ExecuteScalarAsync("SELECT COUNT(*) FROM TestTable");

        using var connection = new FbConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Act
        using var transaction = await connection.BeginTransactionAsync();
        
        var insertSql = "INSERT INTO TestTable (Id, Name, CreatedDate) VALUES (@Id, @Name, @CreatedDate)";
        using var command = new FbCommand(insertSql, connection, transaction);
        
        command.Parameters.Add("@Id", FbDbType.Integer).Value = 999;
        command.Parameters.Add("@Name", FbDbType.VarChar).Value = "Rollback Test";
        command.Parameters.Add("@CreatedDate", FbDbType.TimeStamp).Value = DateTime.Now;
        
        await command.ExecuteNonQueryAsync();
        await transaction.RollbackAsync();

        // Assert
        var finalCount = await _fixture.ExecuteScalarAsync("SELECT COUNT(*) FROM TestTable");
        Assert.Equal(initialCount, finalCount);
    }
}
