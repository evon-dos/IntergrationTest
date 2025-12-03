using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace HIEConnector.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration testing the HIE Connector API.
/// This factory sets up test containers for PostgreSQL and Redis, and configures
/// test-specific service overrides.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RedisContainer _redisContainer;

    public TestWebApplicationFactory()
    {
        // Initialize PostgreSQL test container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("connector_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

        // Initialize Redis test container
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll<DbContextOptions<ConnectorContext>>();
            services.RemoveAll<ConnectorContext>();

            // Add test database context using test container
            services.AddDbContext<ConnectorContext>(options =>
            {
                options.UseNpgsql(_postgresContainer.GetConnectionString());
            });

            // Override SignalR Redis configuration with test container
            var signalRServiceDescriptor = services.FirstOrDefault(d =>
                d.ServiceType.Name.Contains("HubLifetimeManager"));

            if (signalRServiceDescriptor != null)
            {
                services.Remove(signalRServiceDescriptor);
            }

            // Re-configure SignalR with test Redis
            services.AddSignalR(o =>
            {
                o.EnableDetailedErrors = true;
                o.MaximumReceiveMessageSize = null;
            })
            .AddMessagePackProtocol()
            .AddStackExchangeRedis(_redisContainer.GetConnectionString(), o =>
            {
                o.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("connector-test");
            });

            // Mock external service dependencies
            services.RemoveAll<SmileCDRClient>(); // Example of removing real service
            services.AddScoped<SmileCDRClient, MockSmileCDRClient>(); // Add mock

            // Configure test-specific settings
            services.Configure<ApplicationVariables>(options =>
            {
                options.ServiceName = "HIEConnector-Test";
                options.Environment = "Test";
            });

            services.Configure<SignalROptions>(options =>
            {
                options.RedisConnectionString = _redisContainer.GetConnectionString();
                options.HubPath = "/hubs/connector";
            });

            // Build service provider and ensure database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ConnectorContext>();
            db.Database.EnsureCreated();
        });

        // Configure test-specific application settings
        builder.UseEnvironment("Test");
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test-specific configuration
            var testConfig = new Dictionary<string, string>
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317",
                ["ConnectionStrings:ConnectorDb"] = _postgresContainer.GetConnectionString(),
                [$"{SignalROptions.Position}:RedisConnectionString"] = _redisContainer.GetConnectionString(),
                [$"{SignalROptions.Position}:HubPath"] = "/hubs/connector",
                [$"{ApplicationVariables.Position}:ServiceName"] = "HIEConnector-Test",
                [$"{ApplicationVariables.Position}:Environment"] = "Test"
            };

            config.AddInMemoryCollection(testConfig);
        });
    }

    public async Task InitializeAsync()
    {
        // Start test containers before tests run
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        // Stop and cleanup test containers after tests complete
        await _postgresContainer.StopAsync();
        await _redisContainer.StopAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Helper method to get a scoped service from the test server
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Helper method to seed test data into the database
    /// </summary>
    public async Task SeedTestDataAsync(Action<ConnectorContext> seedAction)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConnectorContext>();
        seedAction(context);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Helper method to clean up the database between tests
    /// </summary>
    public async Task CleanupDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConnectorContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Gets the base URL of the test server including scheme, host, and port.
    /// Example: http://localhost:5000/
    /// </summary>
    public Uri GetServerUrl()
    {
        return Server.BaseAddress;
    }

    /// <summary>
    /// Builds a full URL for a specific endpoint relative to the server base URL.
    /// </summary>
    /// <param name="relativeUrl">The relative URL path (e.g., "api/tenants" or "/api/tenants")</param>
    /// <returns>The full URL including server base address</returns>
    public Uri GetEndpointUrl(string relativeUrl)
    {
        return new Uri(Server.BaseAddress, relativeUrl);
    }
}
