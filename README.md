# IntergrationTest

Integration testing example for ASP.NET Core Web API with SignalR and Redis backplane.

## Project Structure

```
├── src/
│   └── SignalRWebApi/              # Web API with SignalR hub
│       ├── Hubs/
│       │   └── ChatHub.cs          # SignalR hub implementation
│       └── Program.cs              # API configuration with SignalR and Redis
└── tests/
    └── SignalRWebApi.IntegrationTests/  # Integration tests
        ├── Fixtures/
        │   └── SignalRWebApplicationFactory.cs  # Test host factory
        ├── ChatHubIntegrationTests.cs           # SignalR hub tests
        ├── RedisIntegrationTests.cs             # Redis integration tests
        └── WebApiIntegrationTests.cs            # API endpoint tests
```

## How Web API and SignalR are Configured in Test Project

This solution demonstrates the **recommended approach** for integration testing ASP.NET Core Web APIs with SignalR and Redis:

### 1. **WebApplicationFactory Pattern**
The test project uses `WebApplicationFactory<Program>` from `Microsoft.AspNetCore.Mvc.Testing` to:
- Host the Web API **in-memory** during tests
- Override configuration (like Redis connection string)
- Provide HTTP clients and test server for SignalR connections

See: `tests/SignalRWebApi.IntegrationTests/Fixtures/SignalRWebApplicationFactory.cs`

### 2. **TestContainers for Redis**
Instead of requiring a manually-started Redis instance, tests use **Testcontainers.Redis** to:
- Automatically spin up a Redis Docker container before tests
- Configure the Web API to use this test Redis instance
- Clean up containers after tests complete

This ensures tests are **isolated** and **repeatable** without external dependencies.

### 3. **SignalR Client Configuration**
Tests create SignalR client connections that connect to the in-memory test server:

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl($"{httpClient.BaseAddress}chathub", options =>
    {
        // Use test server's HTTP handler instead of making real HTTP calls
        options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
    })
    .Build();
```

This allows tests to:
- Connect to SignalR hubs without starting a real web server
- Test real-time messaging between multiple clients
- Verify Redis backplane functionality

## Features Tested

### SignalR Hub Tests (`ChatHubIntegrationTests.cs`)
- ✅ Broadcasting messages to all connected clients
- ✅ Group messaging functionality
- ✅ Joining and leaving groups
- ✅ Connection/disconnection notifications
- ✅ Reconnection capabilities

### Redis Integration Tests (`RedisIntegrationTests.cs`)
- ✅ Redis connectivity
- ✅ Key-value storage and retrieval
- ✅ Pub/Sub messaging
- ✅ Multiple connections
- ✅ SignalR channel prefix configuration

### Web API Tests (`WebApiIntegrationTests.cs`)
- ✅ API endpoint accessibility
- ✅ SignalR hub endpoint negotiation

## Prerequisites

- .NET 9.0 SDK
- Docker (for TestContainers)

## Running the Tests

```bash
# Build the solution
dotnet build

# Run all integration tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~ChatHubIntegrationTests"
```

## Key Configuration Points

### Web API Configuration (`src/SignalRWebApi/Program.cs`)

```csharp
// 1. Add SignalR services
var signalRBuilder = builder.Services.AddSignalR();

// 2. Configure Redis backplane (only if connection string provided)
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    signalRBuilder.AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("SignalRWebApi");
    });
}

// 3. Add CORS for testing
builder.Services.AddCors(/* ... */);

// 4. Map SignalR hub endpoint
app.MapHub<ChatHub>("/chathub");

// 5. Make Program class accessible to tests
public partial class Program { }
```

### Test Factory Configuration

```csharp
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.ConfigureAppConfiguration((context, config) =>
    {
        // Inject test Redis connection string
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Redis"] = RedisConnectionString
        });
    });
}
```

## Benefits of This Approach

1. **No External Dependencies**: Tests manage their own Redis instance
2. **Fast Execution**: In-memory hosting is faster than real HTTP
3. **Isolated Tests**: Each test run has a clean environment
4. **Realistic Testing**: Uses actual SignalR and Redis implementations
5. **Easy CI/CD**: Works anywhere Docker is available

## Production Configuration

For production, configure Redis in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false"
  }
}
```

Or use environment variables:
```bash
ConnectionStrings__Redis="your-redis-connection-string"
```

## License

MIT
