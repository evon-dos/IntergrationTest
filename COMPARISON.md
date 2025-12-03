# Test Factory Pattern Comparison

## Side-by-Side: What Each Factory Does

### Production Application (from problem statement)
```csharp
var builder = WebApplication.CreateBuilder(args);

// Real database
builder.Services.AddDbContext<ConnectorContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("ConnectorDb")));

// Real Redis
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConn, o => { });

// Real services
builder.Services.AddScoped<TenantService>();
builder.Services.AddSingleton<MessageManager>();

var app = builder.Build();
app.Run();
```

### Test Factory (TestWebApplicationFactory)
```csharp
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RedisContainer _redisContainer;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Test database (container)
            services.RemoveAll<ConnectorContext>();
            services.AddDbContext<ConnectorContext>(opt =>
                opt.UseNpgsql(_postgresContainer.GetConnectionString()));

            // Test Redis (container)
            services.AddSignalR()
                .AddStackExchangeRedis(_redisContainer.GetConnectionString());

            // Mock services (if needed)
            services.RemoveAll<MessageManager>();
            services.AddSingleton<MessageManager, MockMessageManager>();
        });
    }
}
```

## Visual Comparison

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    PRODUCTION vs TEST ENVIRONMENT                         │
└──────────────────────────────────────────────────────────────────────────┘

PRODUCTION                               TEST FACTORY
═══════════                              ════════════

Program.cs                               TestWebApplicationFactory.cs
│                                        │
├─ PostgreSQL Database                   ├─ PostgreSQL Container
│  (Real server: prod.db.com)            │  (Local: localhost:5432)
│                                        │
├─ Redis Cache                           ├─ Redis Container
│  (Real cluster: prod.redis.com)        │  (Local: localhost:6379)
│                                        │
├─ External APIs                         ├─ Mock Services
│  └─ SmileCDRClient                     │  └─ MockSmileCDRClient
│     (Real: https://api.smile.com)      │     (Returns fake data)
│                                        │
├─ OpenTelemetry                         ├─ Test Telemetry
│  (Real: otlp://collector:4317)         │  (No-op or local)
│                                        │
└─ SignalR Hub                           └─ SignalR Hub
   (Distributed via Redis)                  (Test Redis backplane)


HOW REQUESTS FLOW:                       HOW TEST REQUESTS FLOW:
═══════════════════                      ═══════════════════════

Browser/Client                           Test Code
    │                                        │
    ├─ HTTPS ──▶ Load Balancer              └─ In-Memory HTTP
    │                │                              │
    │                └─▶ Web Server                 └─▶ Test Server
    │                       │                              │
    │                       ├─ Calls DB                    ├─ Test DB
    │                       ├─ Calls Redis                 ├─ Test Redis
    │                       └─ Calls APIs                  └─ Mock APIs
    │
    └─ Response ◀────────────┘             Response ◀──────┘

DEPLOYMENT:                              EXECUTION:
═══════════                              ═════════

Azure/AWS/On-Prem                        Developer Machine / CI/CD
├─ App Service                           ├─ dotnet test
├─ Managed Database                      ├─ Docker (Testcontainers)
├─ Redis Cache                           └─ In-Process Test Server
└─ Monitoring                                   │
                                                └─ No external network calls
                                                   No manual setup needed
                                                   Isolated per test run
```

## What Changes Between Production and Test

| Component | Production | Test Factory |
|-----------|-----------|--------------|
| **Database** | Real PostgreSQL server | PostgreSQL container OR In-memory |
| **Connection String** | `prod.db.com` | `localhost:5432` (container) |
| **Cache** | Redis cluster | Redis container OR In-memory |
| **SignalR** | Distributed backplane | Test backplane |
| **External APIs** | Real HTTP calls | Mock implementations |
| **Configuration** | appsettings.json | In-memory overrides |
| **Telemetry** | Exported to OTLP | No-op or test collector |
| **Authentication** | Real IdP (Azure AD, etc.) | Test claims/headers |
| **Network** | Internet-facing | Localhost only |
| **Data** | Production data | Test data (seeded) |
| **State** | Persistent | Reset between tests |

## Code Example: Complete Test Class

```csharp
using Xunit;

public class TenantApiTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    // 1. Constructor: Receive factory from xUnit
    public TenantApiTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // 2. Initialize: Clean database before each test
    public async Task InitializeAsync()
    {
        await _factory.CleanupDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // 3. Test: Seed data, make request, verify response
    [Fact]
    public async Task GetTenant_ReturnsCorrectData()
    {
        // Arrange: Seed test data
        await _factory.SeedTestDataAsync(context =>
        {
            context.Tenants.Add(new Tenant
            {
                Id = 1,
                Name = "St. Mary's Hospital",
                Code = "STM001",
                IsActive = true
            });
        });

        // Act: Make HTTP request to test server
        var response = await _client.GetAsync("/api/tenants/1");

        // Assert: Verify response
        response.EnsureSuccessStatusCode();
        var tenant = await response.Content.ReadFromJsonAsync<TenantDto>();
        Assert.NotNull(tenant);
        Assert.Equal("St. Mary's Hospital", tenant.Name);
        Assert.Equal("STM001", tenant.Code);
    }

    [Fact]
    public async Task CreateTenant_SavesToDatabase()
    {
        // Arrange
        var newTenant = new CreateTenantRequest
        {
            Name = "New Hospital",
            Code = "NEW001",
            IsActive = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tenants", newTenant);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        // Verify it was saved to test database
        var getResponse = await _client.GetAsync("/api/tenants");
        var tenants = await getResponse.Content.ReadFromJsonAsync<List<TenantDto>>();
        Assert.Single(tenants);
        Assert.Equal("New Hospital", tenants[0].Name);
    }
}
```

## Key Insight

**The factory makes your test code look almost identical to how you'd use the API manually, but with:**
- Automatic infrastructure setup/teardown
- Isolated test environment
- No manual configuration
- Fast, repeatable tests

## When to Use Which Factory

### Use TestWebApplicationFactory (with containers) for:
✅ End-to-end integration tests
✅ Testing database migrations
✅ Testing complex queries
✅ Testing SignalR functionality
✅ Pre-production validation
✅ When you need high confidence

### Use SimpleTestWebApplicationFactory (in-memory) for:
✅ Fast feedback during development
✅ Testing API contracts
✅ Testing business logic
✅ CI/CD with limited resources
✅ When speed > perfect accuracy

### Use IntegrationTestBase for:
✅ Reducing boilerplate
✅ Standardizing test patterns
✅ When you have many test classes
✅ Sharing setup logic

## Summary

**The test factory is essentially a configured version of your application that:**
1. Replaces production infrastructure with test versions
2. Overrides configuration with test values
3. Provides utilities for seeding/cleaning data
4. Makes it easy to write integration tests
5. Handles all setup/teardown automatically

It's the bridge between "unit test" (testing isolated code) and "manual testing" (using the real app).
