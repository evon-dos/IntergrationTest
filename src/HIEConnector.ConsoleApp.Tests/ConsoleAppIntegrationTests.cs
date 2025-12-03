using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HIEConnector.ConsoleApp.Tests;

/// <summary>
/// Example integration tests for console application using IClassFixture pattern.
/// This demonstrates how to test console app services, background workers, and business logic.
/// </summary>
public class ConsoleAppIntegrationTests : IClassFixture<ConsoleAppTestFixture>, IAsyncLifetime
{
    private readonly ConsoleAppTestFixture _fixture;

    public ConsoleAppIntegrationTests(ConsoleAppTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Clean database before each test
        await _fixture.CleanupDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TenantService_GetTenant_ReturnsCorrectData()
    {
        // Arrange - Seed test data
        await _fixture.SeedTestDataAsync(context =>
        {
            context.Tenants.Add(new Tenant
            {
                Id = 1,
                Name = "Test Hospital",
                Code = "TEST001",
                IsActive = true
            });
        });

        // Act - Get service and call method
        var result = await _fixture.ExecuteInScopeAsync(async sp =>
        {
            var tenantService = sp.GetRequiredService<TenantService>();
            return await tenantService.GetTenantAsync(1);
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Hospital", result.Name);
        Assert.Equal("TEST001", result.Code);
    }

    [Fact]
    public async Task TenantService_CreateTenant_SavesToDatabase()
    {
        // Arrange
        var newTenant = new CreateTenantRequest
        {
            Name = "New Hospital",
            Code = "NEW001",
            IsActive = true
        };

        // Act - Create tenant through service
        await _fixture.ExecuteInScopeAsync(async sp =>
        {
            var tenantService = sp.GetRequiredService<TenantService>();
            await tenantService.CreateTenantAsync(newTenant);
        });

        // Assert - Verify it was saved
        var savedTenant = await _fixture.ExecuteInScopeAsync(async sp =>
        {
            var context = sp.GetRequiredService<ConnectorContext>();
            return await context.Tenants.FirstOrDefaultAsync(t => t.Code == "NEW001");
        });

        Assert.NotNull(savedTenant);
        Assert.Equal("New Hospital", savedTenant.Name);
    }

    [Fact]
    public async Task MessageManager_ProcessMessage_HandlesCorrectly()
    {
        // Arrange
        var messageManager = _fixture.GetService<MessageManager>();
        var testMessage = new Message { Content = "Test", Type = "Info" };

        // Act
        var result = await messageManager.ProcessMessageAsync(testMessage);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task DatabaseContext_Query_WorksCorrectly()
    {
        // Arrange - Seed multiple records
        await _fixture.SeedTestDataAsync(context =>
        {
            context.Tenants.AddRange(
                new Tenant { Name = "Hospital A", Code = "HA001", IsActive = true },
                new Tenant { Name = "Hospital B", Code = "HB002", IsActive = true },
                new Tenant { Name = "Hospital C", Code = "HC003", IsActive = false }
            );
        });

        // Act - Query active tenants
        var activeTenants = await _fixture.ExecuteInScopeAsync(async sp =>
        {
            var context = sp.GetRequiredService<ConnectorContext>();
            return await context.Tenants
                .Where(t => t.IsActive)
                .ToListAsync();
        });

        // Assert
        Assert.Equal(2, activeTenants.Count);
        Assert.All(activeTenants, t => Assert.True(t.IsActive));
    }

    [Fact]
    public async Task MultipleServices_WorkTogether()
    {
        // Arrange - Seed data
        await _fixture.SeedTestDataAsync(context =>
        {
            context.Tenants.Add(new Tenant { Id = 1, Name = "Test", Code = "TEST", IsActive = true });
        });

        // Act - Use multiple services in coordination
        var result = await _fixture.ExecuteInScopeAsync(async sp =>
        {
            var tenantService = sp.GetRequiredService<TenantService>();
            var messageManager = sp.GetRequiredService<MessageManager>();
            var mappingManager = sp.GetRequiredService<MappingFileManager>();

            var tenant = await tenantService.GetTenantAsync(1);
            var message = await messageManager.CreateMessageForTenant(tenant);
            var mapping = mappingManager.GetMappingForTenant(tenant.Code);

            return new { Tenant = tenant, Message = message, Mapping = mapping };
        });

        // Assert
        Assert.NotNull(result.Tenant);
        Assert.NotNull(result.Message);
        Assert.NotNull(result.Mapping);
    }
}

/// <summary>
/// Example tests using the simplified fixture (in-memory, faster)
/// </summary>
public class SimpleConsoleAppIntegrationTests : IClassFixture<SimpleConsoleAppTestFixture>, IAsyncLifetime
{
    private readonly SimpleConsoleAppTestFixture _fixture;

    public SimpleConsoleAppIntegrationTests(SimpleConsoleAppTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.CleanupDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task FastTest_UsingInMemoryDatabase()
    {
        // Arrange
        await _fixture.SeedTestDataAsync(context =>
        {
            context.Tenants.Add(new Tenant { Name = "Fast Test", Code = "FAST", IsActive = true });
        });

        // Act
        var tenant = await _fixture.ExecuteInScopeAsync(async sp =>
        {
            var context = sp.GetRequiredService<ConnectorContext>();
            return await context.Tenants.FirstOrDefaultAsync(t => t.Code == "FAST");
        });

        // Assert
        Assert.NotNull(tenant);
        Assert.Equal("Fast Test", tenant.Name);
    }
}

// Example domain models and services (placeholders)
public class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class TenantDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public class CreateTenantRequest
{
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public class Message
{
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class MessageProcessResult
{
    public bool Success { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

// Service implementations (simplified examples)
public class TenantService
{
    private readonly ConnectorContext _context;
    private readonly TenantRegistrationStore _store;

    public TenantService(ConnectorContext context, TenantRegistrationStore store)
    {
        _context = context;
        _store = store;
    }

    public async Task<TenantDto> GetTenantAsync(int id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Code = tenant.Code,
            IsActive = tenant.IsActive
        };
    }

    public async Task CreateTenantAsync(CreateTenantRequest request)
    {
        var tenant = new Tenant
        {
            Name = request.Name,
            Code = request.Code,
            IsActive = request.IsActive
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
    }
}

public class MessageManager
{
    public async Task<MessageProcessResult> ProcessMessageAsync(Message message)
    {
        await Task.Delay(10); // Simulate processing
        return new MessageProcessResult
        {
            Success = true,
            ProcessedAt = DateTime.UtcNow
        };
    }

    public async Task<Message> CreateMessageForTenant(TenantDto tenant)
    {
        await Task.Delay(5);
        return new Message { Content = $"Message for {tenant.Name}", Type = "Notification" };
    }
}

public class MappingFileManager
{
    public string GetMappingForTenant(string tenantCode)
    {
        return $"Mapping-{tenantCode}";
    }
}

public class TenantRegistrationStore { }
public class SmileCDRClient { }
public class MockSmileCDRClient : SmileCDRClient { }

public class ConnectorContext : DbContext
{
    public ConnectorContext(DbContextOptions<ConnectorContext> options) : base(options) { }
    public DbSet<Tenant> Tenants { get; set; } = null!;
}
