using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HIEConnector.Api.Tests;

/// <summary>
/// Simplified test factory using in-memory databases instead of containers.
/// This is faster for unit-style integration tests but less representative of production.
/// </summary>
public class SimpleTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace real database with in-memory database
            services.RemoveAll<DbContextOptions<ConnectorContext>>();
            services.RemoveAll<ConnectorContext>();

            services.AddDbContext<ConnectorContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });

            // Remove SignalR Redis dependency for simpler testing
            services.RemoveAll(typeof(Microsoft.AspNetCore.SignalR.HubLifetimeManager<>));
            
            // Use in-memory SignalR backplane for tests
            services.AddSignalR(o =>
            {
                o.EnableDetailedErrors = true;
                o.MaximumReceiveMessageSize = null;
            }).AddMessagePackProtocol();

            // Replace external service dependencies with mocks
            ReplaceServiceWithMock<TenantService, MockTenantService>(services);
            ReplaceServiceWithMock<MessageManager, MockMessageManager>(services);
            ReplaceServiceWithMock<MappingFileManager, MockMappingFileManager>(services);

            // Ensure database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ConnectorContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override configuration for tests
            var testConfig = new Dictionary<string, string>
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317",
                ["ConnectionStrings:ConnectorDb"] = "InMemory",
                [$"{SignalROptions.Position}:RedisConnectionString"] = "localhost",
                [$"{SignalROptions.Position}:HubPath"] = "/hubs/connector",
                [$"{ApplicationVariables.Position}:ServiceName"] = "HIEConnector-Test",
                [$"{ApplicationVariables.Position}:Environment"] = "Test"
            };

            config.AddInMemoryCollection(testConfig);
        });
    }

    private static void ReplaceServiceWithMock<TService, TMock>(IServiceCollection services)
        where TService : class
        where TMock : class, TService
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TService));
        if (descriptor != null)
        {
            services.Remove(descriptor);
            services.Add(new ServiceDescriptor(typeof(TService), typeof(TMock), descriptor.Lifetime));
        }
    }

    /// <summary>
    /// Creates an authenticated HTTP client with test user credentials
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string userId = "test-user", string[] roles = null)
    {
        var client = CreateClient();
        // Add authentication headers or tokens here
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId);
        if (roles != null)
        {
            client.DefaultRequestHeaders.Add("X-Test-User-Roles", string.Join(",", roles));
        }
        return client;
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

// Mock service implementations
public class MockTenantService : TenantService
{
    public MockTenantService() : base(null!, null!) { }

    public override Task<TenantDto> GetTenantAsync(int tenantId)
    {
        return Task.FromResult(new TenantDto
        {
            Id = tenantId,
            Name = "Mock Tenant",
            Code = "MOCK001",
            IsActive = true
        });
    }
}

public class MockMessageManager : MessageManager
{
    public MockMessageManager() : base() { }
}

public class MockMappingFileManager : MappingFileManager
{
    public MockMappingFileManager() : base() { }
}

// Base classes for mocks (simplified representations)
public class TenantService
{
    public TenantService(ConnectorContext context, TenantRegistrationStore store) { }
    public virtual Task<TenantDto> GetTenantAsync(int tenantId) => throw new NotImplementedException();
}

public class TenantRegistrationStore { }
public class MessageManager { }
public class MappingFileManager { }
