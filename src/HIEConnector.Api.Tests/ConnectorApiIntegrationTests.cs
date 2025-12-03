using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace HIEConnector.Api.Tests;

/// <summary>
/// Example integration test class showing how to use the TestWebApplicationFactory.
/// This class demonstrates testing API endpoints with a fully configured test environment.
/// </summary>
public class ConnectorApiIntegrationTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ConnectorApiIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Clean database before each test
        await _factory.CleanupDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetTenants_ReturnsEmptyList_WhenNoTenantsExist()
    {
        // Act
        var response = await _client.GetAsync("/api/tenants");

        // Assert
        response.EnsureSuccessStatusCode();
        var tenants = await response.Content.ReadFromJsonAsync<List<TenantDto>>();
        Assert.NotNull(tenants);
        Assert.Empty(tenants);
    }

    [Fact]
    public async Task CreateTenant_ReturnsCreated_WithValidData()
    {
        // Arrange
        var newTenant = new CreateTenantRequest
        {
            Name = "Test Hospital",
            Code = "TEST001",
            IsActive = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tenants", newTenant);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdTenant = await response.Content.ReadFromJsonAsync<TenantDto>();
        Assert.NotNull(createdTenant);
        Assert.Equal(newTenant.Name, createdTenant.Name);
        Assert.Equal(newTenant.Code, createdTenant.Code);
    }

    [Fact]
    public async Task GetTenants_ReturnsSeededData_WhenDataExists()
    {
        // Arrange - Seed test data
        await _factory.SeedTestDataAsync(context =>
        {
            context.Tenants.AddRange(
                new Tenant { Name = "Hospital A", Code = "HA001", IsActive = true },
                new Tenant { Name = "Hospital B", Code = "HB002", IsActive = true }
            );
        });

        // Act
        var response = await _client.GetAsync("/api/tenants");

        // Assert
        response.EnsureSuccessStatusCode();
        var tenants = await response.Content.ReadFromJsonAsync<List<TenantDto>>();
        Assert.NotNull(tenants);
        Assert.Equal(2, tenants.Count);
    }

    [Fact]
    public async Task UpdateTenant_ReturnsNoContent_WhenTenantExists()
    {
        // Arrange
        int tenantId = 0;
        await _factory.SeedTestDataAsync(context =>
        {
            var tenant = new Tenant { Name = "Original Name", Code = "ORG001", IsActive = true };
            context.Tenants.Add(tenant);
            context.SaveChanges();
            tenantId = tenant.Id;
        });

        var updateRequest = new UpdateTenantRequest
        {
            Name = "Updated Name",
            IsActive = false
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tenants/{tenantId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the update
        var getResponse = await _client.GetAsync($"/api/tenants/{tenantId}");
        var updatedTenant = await getResponse.Content.ReadFromJsonAsync<TenantDto>();
        Assert.Equal(updateRequest.Name, updatedTenant?.Name);
        Assert.Equal(updateRequest.IsActive, updatedTenant?.IsActive);
    }

    [Fact]
    public async Task DeleteTenant_ReturnsNoContent_WhenTenantExists()
    {
        // Arrange
        int tenantId = 0;
        await _factory.SeedTestDataAsync(context =>
        {
            var tenant = new Tenant { Name = "To Delete", Code = "DEL001", IsActive = true };
            context.Tenants.Add(tenant);
            context.SaveChanges();
            tenantId = tenant.Id;
        });

        // Act
        var response = await _client.DeleteAsync($"/api/tenants/{tenantId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deletion
        var getResponse = await _client.GetAsync($"/api/tenants/{tenantId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}

// Example DTOs used in tests
public record TenantDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public record CreateTenantRequest
{
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public record UpdateTenantRequest
{
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

// Placeholder classes referenced by the factory
public class ConnectorContext : DbContext
{
    public ConnectorContext(DbContextOptions<ConnectorContext> options) : base(options) { }
    public DbSet<Tenant> Tenants { get; set; } = null!;
}

public class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class SmileCDRClient { }
public class MockSmileCDRClient : SmileCDRClient { }

public class ApplicationVariables
{
    public const string Position = "ApplicationVariables";
    public string ServiceName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
}

public class SignalROptions
{
    public const string Position = "SignalROptions";
    public string RedisConnectionString { get; set; } = string.Empty;
    public string HubPath { get; set; } = string.Empty;
}
