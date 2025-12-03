using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace HIEConnector.ConsoleApp.Tests;

/// <summary>
/// Test fixture for console applications using IClassFixture pattern.
/// Similar to WebApplicationFactory but for console apps with IHost.
/// </summary>
public class ConsoleAppTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RedisContainer _redisContainer;
    private IHost? _host;

    public IServiceProvider Services => _host?.Services 
        ?? throw new InvalidOperationException("Host not initialized. Call InitializeAsync first.");

    public ConsoleAppTestFixture()
    {
        // Initialize test containers
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("connector_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start test containers
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();

        // Build the host with test configuration
        _host = CreateHostBuilder().Build();

        // Initialize database
        using var scope = _host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConnectorContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        await _postgresContainer.StopAsync();
        await _redisContainer.StopAsync();
    }

    /// <summary>
    /// Creates the host builder with test configuration.
    /// This mimics what your Program.cs does but with test overrides.
    /// </summary>
    private IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // Override configuration with test values
                var testConfig = new Dictionary<string, string>
                {
                    ["ConnectionStrings:ConnectorDb"] = _postgresContainer.GetConnectionString(),
                    ["Redis:ConnectionString"] = _redisContainer.GetConnectionString(),
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317",
                    ["ApplicationVariables:ServiceName"] = "HIEConnector-Test",
                    ["ApplicationVariables:Environment"] = "Test"
                };

                config.AddInMemoryCollection(testConfig!);
            })
            .ConfigureServices((context, services) =>
            {
                // Register your services here (same as in Program.cs)
                services.AddDbContext<ConnectorContext>(options =>
                    options.UseNpgsql(context.Configuration.GetConnectionString("ConnectorDb")));

                // Add your application services
                services.AddScoped<TenantService>();
                services.AddScoped<TenantRegistrationStore>();
                services.AddSingleton<MessageManager>();
                services.AddSingleton<MappingFileManager>();

                // Replace external services with mocks for testing
                services.AddScoped<SmileCDRClient, MockSmileCDRClient>();

                // If you have background services/workers, add them here
                // services.AddHostedService<MyWorkerService>();
            });
    }

    /// <summary>
    /// Helper method to execute code within a service scope
    /// </summary>
    public async Task<T> ExecuteInScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Helper method to execute code within a service scope
    /// </summary>
    public async Task ExecuteInScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Helper method to seed test data into the database
    /// </summary>
    public async Task SeedTestDataAsync(Action<ConnectorContext> seedAction)
    {
        await ExecuteInScopeAsync(async sp =>
        {
            var context = sp.GetRequiredService<ConnectorContext>();
            seedAction(context);
            await context.SaveChangesAsync();
        });
    }

    /// <summary>
    /// Helper method to clean up the database between tests
    /// </summary>
    public async Task CleanupDatabaseAsync()
    {
        await ExecuteInScopeAsync(async sp =>
        {
            var context = sp.GetRequiredService<ConnectorContext>();
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        });
    }

    /// <summary>
    /// Helper method to get a service from the DI container
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Helper method to create a new scope (useful for scoped services)
    /// </summary>
    public IServiceScope CreateScope()
    {
        return Services.CreateScope();
    }
}
