# Console App Integration Testing with IClassFixture

This guide shows how to use the `IClassFixture` pattern for testing console applications, similar to how `WebApplicationFactory` is used for Web APIs.

## Key Difference: Web API vs Console App

| Aspect | Web API (WebApplicationFactory) | Console App (Custom Fixture) |
|--------|--------------------------------|------------------------------|
| **Base** | Extends `WebApplicationFactory<Program>` | Uses `IHost` directly |
| **HTTP** | Creates `HttpClient` for requests | No HTTP - direct service calls |
| **Testing** | Tests endpoints via HTTP | Tests services directly |
| **DI Access** | Via `Services` or `CreateClient()` | Via `Services` or scoped execution |

## Console App Test Fixture Implementation

### Full Version (with Testcontainers)

```csharp
public class ConsoleAppTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private IHost? _host;

    public IServiceProvider Services => _host?.Services 
        ?? throw new InvalidOperationException("Host not initialized");

    public ConsoleAppTestFixture()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start containers
        await _postgresContainer.StartAsync();

        // Build host with test config
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                var testConfig = new Dictionary<string, string>
                {
                    ["ConnectionStrings:ConnectorDb"] = _postgresContainer.GetConnectionString(),
                    // ... other test config
                };
                config.AddInMemoryCollection(testConfig!);
            })
            .ConfigureServices((context, services) =>
            {
                // Register your services (same as Program.cs)
                services.AddDbContext<ConnectorContext>(options =>
                    options.UseNpgsql(context.Configuration.GetConnectionString("ConnectorDb")));
                
                services.AddScoped<TenantService>();
                services.AddSingleton<MessageManager>();
                
                // Mock external services
                services.AddScoped<SmileCDRClient, MockSmileCDRClient>();
            })
            .Build();
    }

    public async Task DisposeAsync()
    {
        if (_host != null) await _host.StopAsync();
        await _postgresContainer.StopAsync();
    }

    // Helper methods
    public async Task<T> ExecuteInScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    public async Task SeedTestDataAsync(Action<ConnectorContext> seedAction)
    {
        await ExecuteInScopeAsync(async sp =>
        {
            var context = sp.GetRequiredService<ConnectorContext>();
            seedAction(context);
            await context.SaveChangesAsync();
        });
    }
}
```

### Simplified Version (In-Memory)

```csharp
public class SimpleConsoleAppTestFixture : IAsyncLifetime
{
    private IHost? _host;

    public IServiceProvider Services => _host?.Services 
        ?? throw new InvalidOperationException("Host not initialized");

    public async Task InitializeAsync()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Use in-memory database
                services.AddDbContext<ConnectorContext>(options =>
                    options.UseInMemoryDatabase("TestDatabase"));
                
                services.AddScoped<TenantService>();
                services.AddSingleton<MessageManager>();
            })
            .Build();

        // Initialize database
        using var scope = _host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConnectorContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host != null) await _host.StopAsync();
    }
}
```

## Usage in Tests

### Example 1: Testing a Service

```csharp
public class ConsoleAppTests : IClassFixture<ConsoleAppTestFixture>, IAsyncLifetime
{
    private readonly ConsoleAppTestFixture _fixture;

    public ConsoleAppTests(ConsoleAppTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.CleanupDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TenantService_GetTenant_ReturnsData()
    {
        // Arrange - Seed test data
        await _fixture.SeedTestDataAsync(context =>
        {
            context.Tenants.Add(new Tenant { Id = 1, Name = "Test Hospital" });
        });

        // Act - Call service method
        var result = await _fixture.ExecuteInScopeAsync(async sp =>
        {
            var service = sp.GetRequiredService<TenantService>();
            return await service.GetTenantAsync(1);
        });

        // Assert
        Assert.Equal("Test Hospital", result.Name);
    }
}
```

### Example 2: Testing Multiple Services Together

```csharp
[Fact]
public async Task MultipleServices_WorkTogether()
{
    // Arrange
    await _fixture.SeedTestDataAsync(context =>
    {
        context.Tenants.Add(new Tenant { Id = 1, Name = "Test", Code = "TEST" });
    });

    // Act - Use multiple services in one scope
    var result = await _fixture.ExecuteInScopeAsync(async sp =>
    {
        var tenantService = sp.GetRequiredService<TenantService>();
        var messageManager = sp.GetRequiredService<MessageManager>();

        var tenant = await tenantService.GetTenantAsync(1);
        var message = await messageManager.CreateMessageForTenant(tenant);

        return new { Tenant = tenant, Message = message };
    });

    // Assert
    Assert.NotNull(result.Tenant);
    Assert.NotNull(result.Message);
}
```

### Example 3: Direct Database Testing

```csharp
[Fact]
public async Task Database_Query_Works()
{
    // Arrange
    await _fixture.SeedTestDataAsync(context =>
    {
        context.Tenants.AddRange(
            new Tenant { Name = "A", IsActive = true },
            new Tenant { Name = "B", IsActive = false }
        );
    });

    // Act
    var activeTenants = await _fixture.ExecuteInScopeAsync(async sp =>
    {
        var context = sp.GetRequiredService<ConnectorContext>();
        return await context.Tenants.Where(t => t.IsActive).ToListAsync();
    });

    // Assert
    Assert.Single(activeTenants);
}
```

## Comparison: Web API vs Console App Testing

### Web API Test Pattern

```csharp
public class ApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetEndpoint_ReturnsSuccess()
    {
        // Test via HTTP
        var response = await _client.GetAsync("/api/tenants");
        response.EnsureSuccessStatusCode();
    }
}
```

### Console App Test Pattern

```csharp
public class ConsoleTests : IClassFixture<ConsoleAppTestFixture>
{
    private readonly ConsoleAppTestFixture _fixture;

    public ConsoleTests(ConsoleAppTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetTenant_ReturnsSuccess()
    {
        // Test service directly
        var result = await _fixture.ExecuteInScopeAsync(async sp =>
        {
            var service = sp.GetRequiredService<TenantService>();
            return await service.GetTenantAsync(1);
        });
        
        Assert.NotNull(result);
    }
}
```

## Key Differences Explained

### 1. **No HTTP Layer**

**Web API:**
- Tests make HTTP requests
- Response is HTTP status + body
- Testing the full stack (routing, middleware, serialization)

**Console App:**
- Tests call services directly
- Response is the actual object
- Testing business logic directly

### 2. **Service Access**

**Web API:**
```csharp
var client = factory.CreateClient();
var response = await client.GetAsync("/api/endpoint");
```

**Console App:**
```csharp
var result = await fixture.ExecuteInScopeAsync(async sp =>
{
    var service = sp.GetRequiredService<MyService>();
    return await service.DoWork();
});
```

### 3. **Host Building**

**Web API:**
- Uses `WebApplicationFactory<Program>`
- Automatically configures Kestrel, middleware, etc.

**Console App:**
- Uses `IHostBuilder` directly
- No web server, just DI container and services

## When to Use Each Fixture Type

### Use ConsoleAppTestFixture (with containers):
✅ Testing database operations  
✅ Testing with realistic infrastructure  
✅ Pre-deployment validation  
✅ Testing background workers/hosted services  

### Use SimpleConsoleAppTestFixture (in-memory):
✅ Fast unit-style integration tests  
✅ Testing business logic  
✅ CI/CD pipelines  
✅ Development iteration  

## Testing Background Services / Workers

If your console app has `IHostedService` or background workers:

```csharp
private IHostBuilder CreateHostBuilder()
{
    return Host.CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            // ... other services ...
            
            // Add your background worker
            services.AddHostedService<MyWorkerService>();
        });
}

[Fact]
public async Task BackgroundWorker_ProcessesData()
{
    // The worker starts automatically when the host is built
    
    // Seed data
    await _fixture.SeedTestDataAsync(context =>
    {
        context.WorkItems.Add(new WorkItem { Status = "Pending" });
    });

    // Wait for processing
    await Task.Delay(TimeSpan.FromSeconds(2));

    // Verify the worker processed it
    var processed = await _fixture.ExecuteInScopeAsync(async sp =>
    {
        var context = sp.GetRequiredService<ConnectorContext>();
        return await context.WorkItems.FirstAsync();
    });

    Assert.Equal("Processed", processed.Status);
}
```

## Summary

The console app `IClassFixture` pattern:
- Uses `IHost` instead of `WebApplicationFactory`
- Tests services directly (no HTTP)
- Uses `ExecuteInScopeAsync()` for scoped services
- Otherwise works the same way (seed data, cleanup, mocks, etc.)

See the example files:
- `ConsoleAppTestFixture.cs` - Full fixture implementation
- `SimpleConsoleAppTestFixture.cs` - Fast in-memory version
- `ConsoleAppIntegrationTests.cs` - Usage examples
