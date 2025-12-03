# HIE Connector Integration Test Factory

This repository demonstrates comprehensive test factory patterns for integration testing an ASP.NET Core Web API (HIE Connector).

## What's a Test Factory?

A **test factory** is a configured version of your application that replaces production infrastructure (databases, caches, external APIs) with test versions, making it easy to write automated integration tests.

## Quick Start: What Do the Factories Look Like?

### 1. Full Integration Factory (with Docker)

```csharp
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RedisContainer _redisContainer;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace real database with test container
            services.AddDbContext<ConnectorContext>(options =>
                options.UseNpgsql(_postgresContainer.GetConnectionString()));

            // Replace external services with mocks
            services.RemoveAll<SmileCDRClient>();
            services.AddScoped<SmileCDRClient, MockSmileCDRClient>();
        });
    }
}
```

### 2. Simple Factory (In-Memory, Fast)

```csharp
public class SimpleTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Use in-memory database for speed
            services.AddDbContext<ConnectorContext>(options =>
                options.UseInMemoryDatabase("TestDatabase"));
        });
    }
}
```

### 3. How to Use in Tests

```csharp
public class MyTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MyTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTenants_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/tenants");
        response.EnsureSuccessStatusCode();
    }
}
```

## Repository Structure

```
/
├── FACTORY_OVERVIEW.md           # Quick answer: What factories look like
├── COMPARISON.md                 # Production vs Test comparison
│
└── src/HIEConnector.Api.Tests/
    ├── README.md                 # Comprehensive documentation
    ├── ARCHITECTURE.md           # Visual diagrams
    │
    ├── TestWebApplicationFactory.cs              # Full factory (containers)
    ├── SimpleTestWebApplicationFactory.cs        # Fast factory (in-memory)
    ├── ConnectorApiIntegrationTests.cs           # Example tests
    │
    └── Fixtures/
        └── IntegrationTestFixture.cs             # Base classes & fixtures
```

## Documentation

| File | Description |
|------|-------------|
| **[FACTORY_OVERVIEW.md](FACTORY_OVERVIEW.md)** | Quick overview answering "what do factories look like?" |
| **[COMPARISON.md](COMPARISON.md)** | Side-by-side: Production vs Test environments |
| **[src/HIEConnector.Api.Tests/README.md](src/HIEConnector.Api.Tests/README.md)** | Complete guide with examples, best practices |
| **[src/HIEConnector.Api.Tests/ARCHITECTURE.md](src/HIEConnector.Api.Tests/ARCHITECTURE.md)** | Architecture diagrams and component interactions |

## Three Factory Patterns

### 1. TestWebApplicationFactory
- **Uses:** Docker containers (PostgreSQL, Redis)
- **Best for:** End-to-end integration tests
- **Pros:** Most production-like, tests migrations
- **Cons:** Slower, requires Docker

### 2. SimpleTestWebApplicationFactory  
- **Uses:** In-memory databases
- **Best for:** Fast feedback during development
- **Pros:** Very fast, no Docker needed
- **Cons:** Less accurate, no migrations

### 3. IntegrationTestFixture
- **Uses:** Either of above + shared setup
- **Best for:** Reducing boilerplate code
- **Pros:** Cleaner tests, reusable patterns
- **Cons:** Slight learning curve

## Key Features

✅ **Automatic Infrastructure** - Containers start/stop automatically  
✅ **Test Isolation** - Each test gets clean database  
✅ **Mock External APIs** - No real API calls  
✅ **Configuration Override** - Test-specific settings  
✅ **Helper Methods** - Seed data, cleanup, get services  
✅ **Multiple Patterns** - Choose based on needs  

## Example Test Flow

```
1. Test starts
   └─▶ Factory initializes (starts containers)

2. Before each test
   └─▶ Database is cleaned/reset

3. Test runs
   ├─▶ Seed test data
   ├─▶ Make HTTP request to test server
   └─▶ Verify response

4. After all tests
   └─▶ Factory disposes (stops containers)
```

## What Gets Replaced in Tests?

| Production | Test Factory |
|-----------|--------------|
| Real PostgreSQL | PostgreSQL container or In-memory |
| Redis cluster | Redis container or In-memory |
| External APIs | Mock implementations |
| Real config | In-memory overrides |
| Production data | Test data (seeded) |

## Technologies Used

- ASP.NET Core 8.0
- xUnit for testing
- WebApplicationFactory for integration tests
- Testcontainers for Docker-based infrastructure
- Entity Framework Core with PostgreSQL
- SignalR with Redis backplane

## Related to HIE Connector API

This test infrastructure is designed for the HIE Connector Web API that uses:
- HIEConnector.ConnectorService
- Entity Framework with PostgreSQL
- SignalR with Redis
- Various connector services (TenantService, MessageManager, etc.)

See the original API structure in the problem statement for context.

---

**Start with:** [FACTORY_OVERVIEW.md](FACTORY_OVERVIEW.md) for a quick answer  
**Deep dive:** [src/HIEConnector.Api.Tests/README.md](src/HIEConnector.Api.Tests/README.md) for complete guide