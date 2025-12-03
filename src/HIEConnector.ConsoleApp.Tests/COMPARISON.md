# Console App vs Web API Test Fixtures - Quick Comparison

## Side-by-Side Code Comparison

### Web API Test Fixture

```csharp
public class TestWebApplicationFactory 
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddDbContext<ConnectorContext>(options =>
                options.UseNpgsql(_postgresContainer.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.StopAsync();
    }
}
```

### Console App Test Fixture

```csharp
public class ConsoleAppTestFixture 
    : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private IHost? _host;

    public IServiceProvider Services => _host?.Services;

    private IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<ConnectorContext>(options =>
                    options.UseNpgsql(_postgresContainer.GetConnectionString()));
            });
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        _host = CreateHostBuilder().Build();
    }

    public async Task DisposeAsync()
    {
        if (_host != null) await _host.StopAsync();
        await _postgresContainer.StopAsync();
    }
}
```

## Architecture Comparison

```
┌──────────────────────────────────────────────────────────────────────┐
│                    WEB API TEST ARCHITECTURE                          │
└──────────────────────────────────────────────────────────────────────┘

Test Code
    │
    └─▶ HttpClient (from factory.CreateClient())
           │
           └─▶ In-Memory Test Server
                  │
                  ├─▶ Controllers
                  ├─▶ Middleware
                  ├─▶ Services
                  └─▶ Database


┌──────────────────────────────────────────────────────────────────────┐
│                  CONSOLE APP TEST ARCHITECTURE                        │
└──────────────────────────────────────────────────────────────────────┘

Test Code
    │
    └─▶ IServiceProvider (from fixture.Services)
           │
           └─▶ IHost
                  │
                  ├─▶ Services (Direct Access)
                  ├─▶ Background Workers (if any)
                  └─▶ Database
```

## Test Method Comparison

### Testing a Web API Endpoint

```csharp
public class ApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTenant_ReturnsSuccess()
    {
        // Arrange: Seed data
        await factory.SeedTestDataAsync(context =>
        {
            context.Tenants.Add(new Tenant { Id = 1, Name = "Test" });
        });

        // Act: Make HTTP request
        var response = await _client.GetAsync("/api/tenants/1");

        // Assert: Check HTTP response
        response.EnsureSuccessStatusCode();
        var tenant = await response.Content.ReadFromJsonAsync<TenantDto>();
        Assert.Equal("Test", tenant.Name);
    }
}
```

### Testing a Console App Service

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
        // Arrange: Seed data
        await _fixture.SeedTestDataAsync(context =>
        {
            context.Tenants.Add(new Tenant { Id = 1, Name = "Test" });
        });

        // Act: Call service directly
        var tenant = await _fixture.ExecuteInScopeAsync(async sp =>
        {
            var service = sp.GetRequiredService<TenantService>();
            return await service.GetTenantAsync(1);
        });

        // Assert: Check returned object
        Assert.NotNull(tenant);
        Assert.Equal("Test", tenant.Name);
    }
}
```

## Key Differences Table

| Aspect | Web API | Console App |
|--------|---------|-------------|
| **Base Class** | `WebApplicationFactory<Program>` | Custom class with `IAsyncLifetime` |
| **Host Type** | `IWebHost` | `IHost` |
| **Override Method** | `ConfigureWebHost()` | `CreateHostBuilder()` |
| **Test Client** | `HttpClient` | Direct service access via DI |
| **Request Pattern** | `await client.GetAsync("/api/...")` | `await fixture.ExecuteInScopeAsync(...)` |
| **Response Type** | `HttpResponseMessage` | Actual service return type |
| **Tests What** | Full stack (routing, middleware, serialization) | Services and business logic |
| **HTTP Layer** | ✅ Yes | ❌ No |
| **Controller Testing** | ✅ Yes | ❌ N/A |
| **Service Testing** | ⚠️ Indirect (via HTTP) | ✅ Direct |
| **Background Workers** | ⚠️ Can test but complex | ✅ Natural fit |

## When to Use Which

### Use Web API Fixture When:
- Testing ASP.NET Core Web APIs
- Need to test HTTP endpoints
- Testing routing, middleware, filters
- Testing request/response serialization
- End-to-end API testing

### Use Console App Fixture When:
- Testing console applications
- Testing background services/workers
- Testing business logic directly
- No HTTP endpoints
- Testing scheduled jobs

## Converting Between Them

If you have a Web API and want to add console app tests:

**1. Change the base:**
```csharp
// From:
public class MyFixture : WebApplicationFactory<Program>

// To:
public class MyFixture : IAsyncLifetime
{
    private IHost? _host;
    public IServiceProvider Services => _host?.Services;
}
```

**2. Replace ConfigureWebHost:**
```csharp
// From:
protected override void ConfigureWebHost(IWebHostBuilder builder)

// To:
private IHostBuilder CreateHostBuilder()
{
    return Host.CreateDefaultBuilder()...
}
```

**3. Replace CreateClient() with ExecuteInScopeAsync():**
```csharp
// From:
var response = await _client.GetAsync("/api/endpoint");

// To:
var result = await _fixture.ExecuteInScopeAsync(async sp =>
{
    var service = sp.GetRequiredService<MyService>();
    return await service.DoWork();
});
```

## Both Can Share Common Patterns

Both fixtures support:
- ✅ Testcontainers for infrastructure
- ✅ In-memory databases for speed
- ✅ Service mocking
- ✅ Configuration overrides
- ✅ Database seeding
- ✅ Test isolation
- ✅ IClassFixture pattern
- ✅ Helper methods

The main difference is **how you access the application logic** - via HTTP for Web APIs, or directly via DI for console apps.
