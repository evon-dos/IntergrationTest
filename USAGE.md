# Usage Examples

This document provides practical examples of how to use the integration test framework for different scenarios.

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~ChatHubIntegrationTests"
dotnet test --filter "FullyQualifiedName~RedisIntegrationTests"
dotnet test --filter "FullyQualifiedName~WebApiIntegrationTests"
```

### Run Specific Test Method
```bash
dotnet test --filter "FullyQualifiedName~ChatHubIntegrationTests.SendMessage_ShouldBroadcastToAllConnectedClients"
```

### Run with Detailed Output
```bash
dotnet test --verbosity detailed
```

## Running the Web API Locally

### Option 1: Without Redis (SignalR only, no backplane)
```bash
cd src/SignalRWebApi
dotnet run
```

The API will start without Redis backplane. SignalR will work but won't scale across multiple instances.

### Option 2: With Redis Using Docker Compose
```bash
# Start Redis
docker-compose up -d

# Run the API with Redis connection string
cd src/SignalRWebApi
export ConnectionStrings__Redis="localhost:6379"
dotnet run
```

### Option 3: With Azure Redis Cache (Production)
```bash
cd src/SignalRWebApi
export ConnectionStrings__Redis="your-azure-redis.redis.cache.windows.net:6380,password=YourPassword,ssl=True,abortConnect=False"
dotnet run
```

## Testing SignalR Hub Manually

You can test the SignalR hub using a simple HTML client:

```html
<!DOCTYPE html>
<html>
<head>
    <title>SignalR Test Client</title>
    <script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>
</head>
<body>
    <h1>SignalR Test Client</h1>
    <input type="text" id="userInput" placeholder="User name" />
    <input type="text" id="messageInput" placeholder="Message" />
    <button onclick="sendMessage()">Send</button>
    <ul id="messagesList"></ul>

    <script>
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("https://localhost:5001/chathub")
            .build();

        connection.on("ReceiveMessage", (user, message) => {
            const li = document.createElement("li");
            li.textContent = `${user}: ${message}`;
            document.getElementById("messagesList").appendChild(li);
        });

        connection.start().catch(err => console.error(err));

        async function sendMessage() {
            const user = document.getElementById("userInput").value;
            const message = document.getElementById("messageInput").value;
            await connection.invoke("SendMessage", user, message);
        }
    </script>
</body>
</html>
```

## Adding New Tests

### Example: Testing a New Hub Method

1. Add method to `ChatHub.cs`:
```csharp
public async Task SendPrivateMessage(string recipientConnectionId, string message)
{
    await Clients.Client(recipientConnectionId).SendAsync("ReceivePrivateMessage", Context.ConnectionId, message);
}
```

2. Add test in `ChatHubIntegrationTests.cs`:
```csharp
[Fact]
public async Task SendPrivateMessage_ShouldDeliverToSpecificClient()
{
    // Arrange
    var tcs = new TaskCompletionSource<(string senderId, string message)>();
    
    _connection2!.On<string, string>("ReceivePrivateMessage", (senderId, message) =>
    {
        tcs.SetResult((senderId, message));
    });

    // Act
    await _connection1!.InvokeAsync("SendPrivateMessage", _connection2.ConnectionId, "Private Hello");

    // Assert
    var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.Equal(_connection1.ConnectionId, result.senderId);
    Assert.Equal("Private Hello", result.message);
}
```

### Example: Testing with Different Redis Configurations

You can create specialized test fixtures for different Redis scenarios:

```csharp
public class RedisClusterWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly RedisContainer _redisContainer;

    public RedisClusterWebApplicationFactory()
    {
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithCommand("redis-server", "--appendonly", "yes", "--maxmemory", "100mb")
            .Build();
    }
    
    // ... rest of implementation
}
```

## Debugging Tests

### Enable Verbose Logging
Add to test constructor:
```csharp
public ChatHubIntegrationTests(SignalRWebApplicationFactory factory)
{
    _factory = factory;
    
    // Enable detailed logging for debugging
    Environment.SetEnvironmentVariable("Logging__LogLevel__Microsoft.AspNetCore.SignalR", "Debug");
}
```

### Attach Debugger to TestContainers
TestContainers log detailed information. Check test output for container logs:
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Keep Containers Running After Test Failure
Modify `SignalRWebApplicationFactory.cs`:
```csharp
public new async Task DisposeAsync()
{
    // Comment out these lines to keep container running for debugging
    // await _redisContainer.StopAsync();
    // await _redisContainer.DisposeAsync();
}
```

Then connect to Redis manually:
```bash
# Get container port
docker ps

# Connect with redis-cli
docker exec -it <container-id> redis-cli
```

## CI/CD Integration

### GitHub Actions Example
```yaml
name: Integration Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run integration tests
      run: dotnet test --no-build --verbosity normal
```

### Azure DevOps Example
```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '9.0.x'

- script: dotnet restore
  displayName: 'Restore dependencies'

- script: dotnet build --no-restore
  displayName: 'Build solution'

- script: dotnet test --no-build --logger trx
  displayName: 'Run integration tests'

- task: PublishTestResults@2
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '**/*.trx'
```

## Production Deployment Considerations

### 1. Redis Connection Resilience
```csharp
signalRBuilder.AddStackExchangeRedis(redisConnectionString, options =>
{
    options.Configuration.ChannelPrefix = RedisChannel.Literal("SignalRWebApi");
    options.Configuration.AbortOnConnectFail = false;
    options.Configuration.ConnectRetry = 3;
    options.Configuration.ReconnectRetryPolicy = new ExponentialRetry(5000);
});
```

### 2. Load Balancing
When using Redis backplane, you can run multiple instances of the API:
```bash
# Terminal 1
export ASPNETCORE_URLS="http://localhost:5001"
dotnet run

# Terminal 2
export ASPNETCORE_URLS="http://localhost:5002"
dotnet run
```

All instances will share messages through Redis.

### 3. Monitoring
Add Application Insights or similar:
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### 4. Health Checks
Add health checks for Redis:
```csharp
builder.Services.AddHealthChecks()
    .AddRedis(redisConnectionString, name: "redis");

app.MapHealthChecks("/health");
```
