using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;

namespace SignalRWebApi.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory that configures the Web API with a Redis TestContainer
/// This demonstrates how to configure Web API and SignalR in test project
/// </summary>
public class SignalRWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;

    public SignalRWebApplicationFactory()
    {
        // Initialize Redis container for testing
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithPortBinding(6379, true)
            .Build();
    }

    /// <summary>
    /// Gets the Redis connection string for tests
    /// </summary>
    public string RedisConnectionString => _redisContainer.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override the Redis connection string with TestContainer connection
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = RedisConnectionString
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Additional test-specific service configuration can go here
            // For example, you could replace services with mocks if needed
        });
    }

    public async Task InitializeAsync()
    {
        // Start the Redis container before tests
        await _redisContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        // Stop and cleanup the Redis container after tests
        await _redisContainer.StopAsync();
        await _redisContainer.DisposeAsync();
    }
}
