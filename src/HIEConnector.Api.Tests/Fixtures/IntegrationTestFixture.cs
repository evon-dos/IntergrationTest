using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HIEConnector.Api.Tests.Fixtures;

/// <summary>
/// Base fixture for integration tests that provides common setup and teardown.
/// Use this when you need shared initialization across multiple test classes.
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    public TestWebApplicationFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Factory = new TestWebApplicationFactory();
        await Factory.InitializeAsync();
        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Factory != null)
        {
            await Factory.DisposeAsync();
        }
    }

    /// <summary>
    /// Helper to execute an action within a service scope
    /// </summary>
    public async Task<T> ExecuteInScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Helper to execute an action within a service scope
    /// </summary>
    public async Task ExecuteInScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        await action(scope.ServiceProvider);
    }
}

/// <summary>
/// Collection fixture for tests that can share the same application instance.
/// This improves test performance by reusing the same factory across multiple test classes.
/// </summary>
[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

/// <summary>
/// Base class for integration tests that provides common helper methods.
/// Inherit from this to get access to factory, client, and utility methods.
/// </summary>
[Collection("Integration Tests")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected TestWebApplicationFactory Factory { get; }
    protected HttpClient Client { get; }

    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        Factory = fixture.Factory;
        Client = fixture.Client;
    }

    public virtual async Task InitializeAsync()
    {
        // Clean up database before each test
        await Factory.CleanupDatabaseAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Seeds test data into the database
    /// </summary>
    protected async Task SeedAsync(Action<ConnectorContext> seedAction)
    {
        await Factory.SeedTestDataAsync(seedAction);
    }

    /// <summary>
    /// Gets a service from the DI container
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return Factory.GetService<T>();
    }

    /// <summary>
    /// Creates a new HTTP client for this test
    /// </summary>
    protected HttpClient CreateClient()
    {
        return Factory.CreateClient();
    }
}

/// <summary>
/// Example test class using the base integration test
/// </summary>
public class ExampleIntegrationTests : IntegrationTestBase
{
    public ExampleIntegrationTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Example_Test_Using_Base_Class()
    {
        // Arrange - use helper methods from base class
        await SeedAsync(context =>
        {
            context.Tenants.Add(new Tenant
            {
                Name = "Test Hospital",
                Code = "TEST001",
                IsActive = true
            });
        });

        // Act
        var response = await Client.GetAsync("/api/tenants");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
