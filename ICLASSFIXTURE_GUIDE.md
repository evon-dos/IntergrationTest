# IClassFixture Pattern for Integration Testing

This repository demonstrates the `IClassFixture` pattern for integration testing in .NET, covering both **Web APIs** and **Console Applications**.

## What is IClassFixture?

`IClassFixture<T>` is an xUnit pattern that:
- Creates a single instance of `T` shared across all tests in a class
- Initializes once before tests run
- Disposes after all tests complete
- Provides test isolation with shared infrastructure

## Two Application Types

### 1. Web API Testing

Uses `WebApplicationFactory<Program>` to test HTTP endpoints.

**Location:** `src/HIEConnector.Api.Tests/`

**Key Pattern:**
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
        var response = await _client.GetAsync("/api/tenants");
        response.EnsureSuccessStatusCode();
    }
}
```

### 2. Console App Testing

Uses `IHost` to test services directly (no HTTP).

**Location:** `src/HIEConnector.ConsoleApp.Tests/`

**Key Pattern:**
```csharp
public class ConsoleTests : IClassFixture<ConsoleAppTestFixture>
{
    private readonly ConsoleAppTestFixture _fixture;
    
    public ConsoleTests(ConsoleAppTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task ServiceMethod_Works()
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

## Quick Comparison

| Feature | Web API | Console App |
|---------|---------|-------------|
| **Base** | `WebApplicationFactory<Program>` | `IHost` + `IAsyncLifetime` |
| **Testing** | Via HTTP (`HttpClient`) | Direct service calls |
| **Access** | `factory.CreateClient()` | `fixture.ExecuteInScopeAsync()` |
| **Tests** | Endpoints, routing, middleware | Services, business logic |
| **Response** | `HttpResponseMessage` | Actual object |

## Repository Structure

```
/
├── README.md                              # Main overview
├── FACTORY_OVERVIEW.md                    # Quick factory guide
├── COMPARISON.md                          # Production vs Test
│
├── src/HIEConnector.Api.Tests/            # WEB API PATTERNS
│   ├── README.md                          # Web API guide
│   ├── ARCHITECTURE.md                    # Diagrams
│   ├── TestWebApplicationFactory.cs       # Full (containers)
│   ├── SimpleTestWebApplicationFactory.cs # Fast (in-memory)
│   ├── ConnectorApiIntegrationTests.cs    # Examples
│   └── Fixtures/
│       └── IntegrationTestFixture.cs      # Base classes
│
└── src/HIEConnector.ConsoleApp.Tests/     # CONSOLE APP PATTERNS
    ├── README.md                          # Console app guide
    ├── COMPARISON.md                      # Web vs Console
    ├── ConsoleAppTestFixture.cs           # Full (containers)
    ├── SimpleConsoleAppTestFixture.cs     # Fast (in-memory)
    └── ConsoleAppIntegrationTests.cs      # Examples
```

## Common Features (Both Types)

Both patterns support:
✅ Testcontainers (PostgreSQL, Redis)  
✅ In-memory databases for speed  
✅ Service mocking  
✅ Configuration overrides  
✅ Database seeding  
✅ Test isolation  
✅ Cleanup between tests  

## Getting Started

### For Web API Testing
1. Read: [`src/HIEConnector.Api.Tests/README.md`](src/HIEConnector.Api.Tests/README.md)
2. Use: `TestWebApplicationFactory` or `SimpleTestWebApplicationFactory`
3. Test: HTTP endpoints via `HttpClient`

### For Console App Testing
1. Read: [`src/HIEConnector.ConsoleApp.Tests/README.md`](src/HIEConnector.ConsoleApp.Tests/README.md)
2. Use: `ConsoleAppTestFixture` or `SimpleConsoleAppTestFixture`
3. Test: Services directly via `ExecuteInScopeAsync()`

## Example: Converting Between Patterns

If you have Web API tests and want console app tests:

**Change 1:** The fixture
```csharp
// From:
public class TestFixture : WebApplicationFactory<Program> { }

// To:
public class TestFixture : IAsyncLifetime
{
    private IHost? _host;
    public IServiceProvider Services => _host?.Services;
}
```

**Change 2:** The test
```csharp
// From:
var response = await _client.GetAsync("/api/tenants/1");
var tenant = await response.Content.ReadFromJsonAsync<TenantDto>();

// To:
var tenant = await _fixture.ExecuteInScopeAsync(async sp =>
{
    var service = sp.GetRequiredService<TenantService>();
    return await service.GetTenantAsync(1);
});
```

## Documentation Index

| Document | Description |
|----------|-------------|
| **Main Guides** |
| [README.md](README.md) | This file - overview and navigation |
| [FACTORY_OVERVIEW.md](FACTORY_OVERVIEW.md) | Quick reference for factories |
| [COMPARISON.md](COMPARISON.md) | Production vs Test environments |
| **Web API Testing** |
| [Api.Tests/README.md](src/HIEConnector.Api.Tests/README.md) | Complete Web API testing guide |
| [Api.Tests/ARCHITECTURE.md](src/HIEConnector.Api.Tests/ARCHITECTURE.md) | Architecture diagrams |
| **Console App Testing** |
| [ConsoleApp.Tests/README.md](src/HIEConnector.ConsoleApp.Tests/README.md) | Complete console app testing guide |
| [ConsoleApp.Tests/COMPARISON.md](src/HIEConnector.ConsoleApp.Tests/COMPARISON.md) | Web vs Console comparison |

## Key Takeaway

The `IClassFixture` pattern works for both Web APIs and Console Apps:
- **Web APIs**: Test via HTTP using `WebApplicationFactory`
- **Console Apps**: Test services directly using `IHost`

Both provide the same benefits: test isolation, infrastructure automation, and clean test code.
