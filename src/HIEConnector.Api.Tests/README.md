# HIE Connector Integration Test Factory Patterns

This document describes the test factory patterns used for integration testing the HIE Connector API.

## Overview

The test factories provide a way to configure and run integration tests against the HIE Connector Web API with proper test isolation, dependency management, and infrastructure setup.

## Factory Types

### 1. TestWebApplicationFactory (Full Integration)

**File:** `TestWebApplicationFactory.cs`

This is the most comprehensive factory that uses actual test containers (Docker) to provide realistic test environments.

**Features:**
- Uses Testcontainers for PostgreSQL and Redis
- Most accurate representation of production environment
- Proper database and cache isolation between test runs
- Slower but more reliable

**When to use:**
- End-to-end integration tests
- Testing database migrations
- Testing SignalR real-time features
- Pre-deployment validation tests

**Example Usage:**
```csharp
public class MyIntegrationTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MyIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.CleanupDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Test_Something()
    {
        // Arrange
        await _factory.SeedTestDataAsync(context =>
        {
            context.Tenants.Add(new Tenant { Name = "Test" });
        });

        // Act
        var response = await _client.GetAsync("/api/tenants");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
```

**Key Methods:**
- `InitializeAsync()` - Starts test containers
- `CleanupDatabaseAsync()` - Resets database between tests
- `SeedTestDataAsync(action)` - Seeds test data
- `GetService<T>()` - Gets services from DI container
- `GetServerUrl()` - Gets the test server URL with port
- `GetEndpointUrl(path)` - Builds full URL for an endpoint

### 2. SimpleTestWebApplicationFactory (In-Memory)

**File:** `SimpleTestWebApplicationFactory.cs`

A lightweight factory that uses in-memory databases and removes external dependencies.

**Features:**
- Uses EF Core In-Memory database
- No Docker/containers required
- Fast test execution
- Less representative of production

**When to use:**
- Unit-style integration tests
- Testing business logic
- Rapid feedback during development
- CI/CD pipelines with limited resources

**Example Usage:**
```csharp
public class QuickTests : IClassFixture<SimpleTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public QuickTests(SimpleTestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Quick_Test()
    {
        var response = await _client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
    }
}
```

**Key Methods:**
- `CreateAuthenticatedClient(userId, roles)` - Creates client with test auth
- `GetServerUrl()` - Gets the test server URL with port
- `GetEndpointUrl(path)` - Builds full URL for an endpoint
- `ReplaceServiceWithMock<TService, TMock>()` - Swaps real services with mocks

### 3. IntegrationTestFixture (Shared Setup)

**File:** `Fixtures/IntegrationTestFixture.cs`

A reusable fixture that provides shared setup across multiple test classes.

**Features:**
- Implements xUnit IClassFixture and ICollectionFixture patterns
- Provides base class for common test operations
- Reduces boilerplate code
- Enables test collection sharing

**When to use:**
- When multiple test classes need the same setup
- To improve test performance via shared factory instances
- To standardize test patterns across the project

**Example Usage:**

**Option 1: Using IntegrationTestBase**
```csharp
public class MyTests : IntegrationTestBase
{
    public MyTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Test_With_Base_Helpers()
    {
        // Use helper methods from base class
        await SeedAsync(context => { /* seed data */ });
        
        var response = await Client.GetAsync("/api/endpoint");
        
        Assert.True(response.IsSuccessStatusCode);
    }
}
```

**Option 2: Using Collection Fixture**
```csharp
[Collection("Integration Tests")]
public class SharedTests
{
    private readonly IntegrationTestFixture _fixture;

    public SharedTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Shared_Test()
    {
        var result = await _fixture.ExecuteInScopeAsync(async sp =>
        {
            var service = sp.GetRequiredService<TenantService>();
            return await service.GetTenantAsync(1);
        });
        
        Assert.NotNull(result);
    }
}
```

## Architecture

### How It Works

1. **Factory Initialization**
   - Test containers are started (PostgreSQL, Redis)
   - Configuration is overridden with test values
   - Services are registered/replaced with test versions

2. **Per-Test Setup**
   - Database is cleaned/reset
   - Test data is seeded as needed
   - HTTP client is created from factory

3. **Test Execution**
   - Tests run against the configured application
   - All infrastructure is isolated per test
   - Actual HTTP calls are made to test endpoints

4. **Cleanup**
   - Database is reset between tests
   - Containers are stopped after all tests complete
   - Resources are properly disposed

### Dependency Management

The factories handle several types of dependencies:

**Database:**
- Production: PostgreSQL via connection string
- Test: PostgreSQL container OR In-Memory EF Core

**Cache/SignalR:**
- Production: Redis cluster
- Test: Redis container OR In-Memory backplane

**External Services:**
- Production: Real API clients (SmileCDR, etc.)
- Test: Mock implementations

### Configuration Override

Test factories override configuration in `ConfigureWebHost`:

```csharp
builder.ConfigureAppConfiguration((context, config) =>
{
    var testConfig = new Dictionary<string, string>
    {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317",
        ["ConnectionStrings:ConnectorDb"] = _postgresContainer.GetConnectionString(),
        // ... more overrides
    };
    config.AddInMemoryCollection(testConfig);
});
```

## Best Practices

### 1. Getting the Test Server URL

To access the test server URL with port in your tests:

```csharp
// Method 1: Using the helper method
var serverUrl = factory.GetServerUrl();
Console.WriteLine($"Server: {serverUrl}"); // e.g., http://localhost/

// Method 2: Direct access
var serverUrl = factory.Server.BaseAddress;

// Method 3: From HttpClient
var client = factory.CreateClient();
var serverUrl = client.BaseAddress;

// Building endpoint URLs
var tenantsUrl = factory.GetEndpointUrl("api/tenants");
var hubUrl = new Uri(serverUrl, "/hubs/connector");
```

**For SignalR connections:**
```csharp
var hubUrl = new Uri(factory.GetServerUrl(), "/hubs/connector");
var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl, options =>
    {
        options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
    })
    .Build();
```

**Important:** The test server is in-memory by default. `Server.BaseAddress` provides a URL, but no actual TCP port is opened. Use `factory.CreateClient()` for HTTP requests.

See [`GET_SERVER_URL.md`](GET_SERVER_URL.md) for complete examples and [`ServerUrlExamples.cs`](ServerUrlExamples.cs) for working code.

### 2. Database Isolation
Always clean the database between tests:
```csharp
public async Task InitializeAsync()
{
    await _factory.CleanupDatabaseAsync();
}
```

### 3. Test Data Seeding
Use the factory's seed method for consistency:
```csharp
await _factory.SeedTestDataAsync(context =>
{
    context.Tenants.AddRange(
        new Tenant { Name = "Hospital A" },
        new Tenant { Name = "Hospital B" }
    );
});
```

### 4. Service Mocking
Replace external dependencies with mocks:
```csharp
builder.ConfigureTestServices(services =>
{
    services.RemoveAll<ExternalService>();
    services.AddScoped<ExternalService, MockExternalService>();
});
```

### 5. Parallel Test Execution
Use collection fixtures to control test parallelization:
```csharp
[Collection("Integration Tests")]  // Tests in same collection run sequentially
public class MyTests { }
```

## Dependencies

Required NuGet packages:

```xml
<ItemGroup>
    <!-- Testing Framework -->
    <PackageReference Include="xunit" Version="2.6.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
    
    <!-- Test Factory -->
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
    
    <!-- Test Containers (for full integration) -->
    <PackageReference Include="Testcontainers.PostgreSql" Version="3.7.0" />
    <PackageReference Include="Testcontainers.Redis" Version="3.7.0" />
    
    <!-- In-Memory Database (for simple tests) -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
</ItemGroup>
```

## File Structure

```
HIEConnector.Api.Tests/
├── TestWebApplicationFactory.cs           # Full test factory with containers
├── SimpleTestWebApplicationFactory.cs     # Lightweight in-memory factory
├── Fixtures/
│   └── IntegrationTestFixture.cs         # Shared fixture and base classes
├── ConnectorApiIntegrationTests.cs       # Example integration tests
└── HIEConnector.Api.Tests.csproj         # Test project file
```

## Common Scenarios

### Testing Authentication/Authorization
```csharp
var client = _factory.CreateAuthenticatedClient("user123", new[] { "Admin" });
var response = await client.GetAsync("/api/secure-endpoint");
```

### Testing SignalR Hubs
```csharp
var hubConnection = new HubConnectionBuilder()
    .WithUrl($"{_factory.Server.BaseAddress}hubs/connector", options =>
    {
        options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
    })
    .Build();

await hubConnection.StartAsync();
```

### Testing with Background Services
```csharp
// Factory automatically starts hosted services
// Wait for processing
await Task.Delay(TimeSpan.FromSeconds(2));

// Verify results
var result = await _client.GetAsync("/api/processed-data");
```

## Troubleshooting

### Tests are slow
- Use `SimpleTestWebApplicationFactory` for faster tests
- Share factory instances via collection fixtures
- Reduce test data to minimum needed

### Database state persists between tests
- Ensure `CleanupDatabaseAsync()` is called in `InitializeAsync()`
- Check that tests are not running in parallel when they shouldn't

### Containers fail to start
- Ensure Docker is running
- Check port availability
- Review container logs: `docker logs <container-id>`

## See Also

- [ASP.NET Core Integration Tests](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [xUnit Collection Fixtures](https://xunit.net/docs/shared-context)
- [Testcontainers Documentation](https://dotnet.testcontainers.org/)
