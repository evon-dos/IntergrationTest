# What the Test Factory Looks Like

## Quick Answer

For the HIE Connector Web API shown in the problem statement, here are the test factories:

## 1. Full Integration Test Factory (with Docker Containers)

```csharp
public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RedisContainer _redisContainer;

    public TestWebApplicationFactory()
    {
        // Initialize test containers
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("connector_test")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace real database with test database
            services.RemoveAll<ConnectorContext>();
            services.AddDbContext<ConnectorContext>(options =>
                options.UseNpgsql(_postgresContainer.GetConnectionString()));

            // Replace SignalR Redis with test Redis
            services.AddSignalR()
                .AddStackExchangeRedis(_redisContainer.GetConnectionString());

            // Replace external services with mocks
            services.RemoveAll<SmileCDRClient>();
            services.AddScoped<SmileCDRClient, MockSmileCDRClient>();
        });
    }

    public async Task InitializeAsync()
    {
        // Start containers before tests
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        // Stop containers after tests
        await _postgresContainer.StopAsync();
        await _redisContainer.StopAsync();
    }
}
```

**Use this factory when:**
- You need the most production-like environment
- Testing database migrations or complex queries
- Testing SignalR real-time features
- Running pre-deployment validation

## 2. Simple Test Factory (In-Memory, No Docker)

```csharp
public class SimpleTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace real database with in-memory database
            services.RemoveAll<ConnectorContext>();
            services.AddDbContext<ConnectorContext>(options =>
                options.UseInMemoryDatabase("TestDatabase"));

            // Use in-memory SignalR (no Redis)
            services.AddSignalR();

            // Replace all external services with mocks
            services.RemoveAll<TenantService>();
            services.AddScoped<TenantService, MockTenantService>();
        });

        builder.UseEnvironment("Test");
    }

    public HttpClient CreateAuthenticatedClient(string userId = "test-user")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId);
        return client;
    }
}
```

**Use this factory when:**
- You want fast test execution
- Running tests in CI/CD without Docker
- Testing business logic and API contracts
- Rapid feedback during development

## 3. How to Use the Factories in Tests

### Basic Test Example

```csharp
public class MyIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MyIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTenants_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/tenants");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
```

### Test with Data Seeding

```csharp
[Fact]
public async Task GetTenant_ReturnsSeededData()
{
    // Arrange - Seed test data
    await _factory.SeedTestDataAsync(context =>
    {
        context.Tenants.Add(new Tenant
        {
            Name = "Test Hospital",
            Code = "TEST001",
            IsActive = true
        });
    });

    // Act
    var response = await _client.GetAsync("/api/tenants/1");

    // Assert
    response.EnsureSuccessStatusCode();
    var tenant = await response.Content.ReadFromJsonAsync<TenantDto>();
    Assert.Equal("Test Hospital", tenant.Name);
}
```

### Using Base Class Pattern

```csharp
public class MyTests : IntegrationTestBase
{
    public MyTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Test_With_Helpers()
    {
        // Use helper methods from base class
        await SeedAsync(context => { /* seed data */ });
        
        var response = await Client.GetAsync("/api/tenants");
        
        Assert.True(response.IsSuccessStatusCode);
    }
}
```

## Key Factory Methods

All factories provide these essential methods:

```csharp
// Create HTTP client for making requests
HttpClient client = factory.CreateClient();

// Seed test data into database
await factory.SeedTestDataAsync(context => {
    context.Tenants.Add(new Tenant { Name = "Test" });
});

// Clean database between tests
await factory.CleanupDatabaseAsync();

// Get service from DI container
var service = factory.GetService<TenantService>();
```

## Architecture Summary

```
Test Factory
    │
    ├─ Starts test infrastructure (containers or in-memory)
    ├─ Overrides configuration with test values
    ├─ Replaces real services with mocks
    ├─ Provides HTTP client for testing
    │
    └─ Your Tests
        ├─ Seed data
        ├─ Make HTTP requests
        ├─ Verify responses
        └─ Clean up
```

## Files Location

- **TestWebApplicationFactory.cs** - Full factory with Docker containers
- **SimpleTestWebApplicationFactory.cs** - Lightweight in-memory factory
- **Fixtures/IntegrationTestFixture.cs** - Base classes and shared setup
- **ConnectorApiIntegrationTests.cs** - Example tests
- **README.md** - Complete documentation
- **ARCHITECTURE.md** - Visual diagrams and architecture

## That's It!

The test factories handle all the complexity of:
- Starting/stopping infrastructure (databases, caches)
- Configuring the application for testing
- Providing clean, isolated test environments
- Making it easy to write integration tests

Just inherit from the factory and start writing tests!
