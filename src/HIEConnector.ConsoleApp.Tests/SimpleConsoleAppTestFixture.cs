using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HIEConnector.ConsoleApp.Tests;

/// <summary>
/// Simplified test fixture for console applications using in-memory databases.
/// Faster than container-based testing, suitable for unit-style integration tests.
/// </summary>
public class SimpleConsoleAppTestFixture : IAsyncLifetime
{
    private IHost? _host;

    public IServiceProvider Services => _host?.Services 
        ?? throw new InvalidOperationException("Host not initialized. Call InitializeAsync first.");

    public async Task InitializeAsync()
    {
        // Build the host with test configuration
        _host = CreateHostBuilder().Build();

        // Initialize in-memory database
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
    }

    private IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // Override configuration with test values
                var testConfig = new Dictionary<string, string>
                {
                    ["ConnectionStrings:ConnectorDb"] = "InMemory",
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317",
                    ["ApplicationVariables:ServiceName"] = "HIEConnector-Test",
                    ["ApplicationVariables:Environment"] = "Test"
                };

                config.AddInMemoryCollection(testConfig!);
            })
            .ConfigureServices((context, services) =>
            {
                // Use in-memory database instead of real PostgreSQL
                services.AddDbContext<ConnectorContext>(options =>
                    options.UseInMemoryDatabase("TestDatabase"));

                // Add your application services
                services.AddScoped<TenantService>();
                services.AddScoped<TenantRegistrationStore>();
                services.AddSingleton<MessageManager>();
                services.AddSingleton<MappingFileManager>();

                // Mock external services
                services.AddScoped<SmileCDRClient, MockSmileCDRClient>();
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
    /// Helper method to seed test data
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
}
