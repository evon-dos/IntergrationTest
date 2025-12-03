# Getting Test Server URL and Port in Integration Tests

When using `WebApplicationFactory` for integration tests, you can access the test server URL in several ways.

## Quick Answer

### Method 1: Using Server.BaseAddress (Recommended)

The simplest way to get the test server URL:

```csharp
public class MyTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public MyTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void GetServerUrl_ReturnsBaseAddress()
    {
        // Get the test server URL with port
        var serverUrl = _factory.Server.BaseAddress;
        
        // Example: http://localhost:5000/ or similar
        Assert.NotNull(serverUrl);
        Console.WriteLine($"Test server URL: {serverUrl}");
    }
}
```

### Method 2: Using HttpClient.BaseAddress

When you create a client from the factory:

```csharp
[Fact]
public void GetServerUrl_FromClient()
{
    var client = _factory.CreateClient();
    var serverUrl = client.BaseAddress;
    
    // Example: http://localhost:5000/
    Console.WriteLine($"Server URL: {serverUrl}");
}
```

### Method 3: For SignalR or WebSocket Connections

When you need the URL for SignalR or other connections:

```csharp
[Fact]
public async Task ConnectToSignalRHub_UsingServerUrl()
{
    // Get the server URL
    var serverUrl = _factory.Server.BaseAddress;
    
    // Build SignalR connection URL
    var hubUrl = $"{serverUrl}hubs/connector";
    
    var hubConnection = new HubConnectionBuilder()
        .WithUrl(hubUrl, options =>
        {
            // Use the factory's handler for authentication/cookies
            options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
        })
        .Build();

    await hubConnection.StartAsync();
    Assert.Equal(HubConnectionState.Connected, hubConnection.State);
}
```

## Complete Examples

### Example 1: Simple URL Access

```csharp
public class ServerUrlTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ServerUrlTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Server_HasValidBaseAddress()
    {
        // Arrange & Act
        var baseAddress = _factory.Server.BaseAddress;

        // Assert
        Assert.NotNull(baseAddress);
        Assert.True(baseAddress.IsAbsoluteUri);
        Console.WriteLine($"Test Server URL: {baseAddress}");
        Console.WriteLine($"Scheme: {baseAddress.Scheme}");
        Console.WriteLine($"Host: {baseAddress.Host}");
        Console.WriteLine($"Port: {baseAddress.Port}");
    }

    [Fact]
    public void Client_HasMatchingBaseAddress()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var serverUrl = _factory.Server.BaseAddress;
        var clientUrl = client.BaseAddress;

        // Assert
        Assert.Equal(serverUrl, clientUrl);
    }
}
```

### Example 2: Building URLs for Different Endpoints

```csharp
[Fact]
public async Task MakeRequest_UsingBuiltUrl()
{
    // Get server base address
    var baseUrl = _factory.Server.BaseAddress;
    
    // Build specific endpoint URLs
    var tenantsUrl = new Uri(baseUrl, "api/tenants");
    var healthUrl = new Uri(baseUrl, "health");
    
    var client = _factory.CreateClient();
    
    // Make requests
    var response1 = await client.GetAsync(tenantsUrl);
    var response2 = await client.GetAsync(healthUrl);
    
    Assert.True(response1.IsSuccessStatusCode);
    Assert.True(response2.IsSuccessStatusCode);
}
```

### Example 3: Custom Port Configuration

If you need to specify a custom port:

```csharp
public class CustomPortFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseUrls("http://localhost:5555");
        
        builder.ConfigureTestServices(services =>
        {
            // Your test configuration
        });
    }
}

// Usage
[Fact]
public void CustomPort_IsUsed()
{
    var factory = new CustomPortFactory();
    var serverUrl = factory.Server.BaseAddress;
    
    Assert.Equal(5555, serverUrl.Port);
}
```

### Example 4: SignalR Hub Connection with Server URL

```csharp
public class SignalRTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SignalRTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConnectToHub_UsingServerUrl()
    {
        // Get the server URL
        var serverUrl = _factory.Server.BaseAddress;
        
        // Build hub URL
        var hubPath = "/hubs/connector";
        var hubUrl = new Uri(serverUrl, hubPath);
        
        Console.WriteLine($"Connecting to SignalR hub at: {hubUrl}");

        // Create connection
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // Important: Use the factory's handler
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        // Connect
        await connection.StartAsync();

        // Assert
        Assert.Equal(HubConnectionState.Connected, connection.State);

        // Cleanup
        await connection.StopAsync();
    }
}
```

### Example 5: External Client Connection (Advanced)

If you need to test with an actual external client (not recommended for most cases):

```csharp
public class ExternalClientTests : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task ExternalClient_CanConnect()
    {
        // Note: WebApplicationFactory uses an in-memory test server by default
        // For external clients, you need WebApplicationFactory with a real server
        
        var factory = new TestWebApplicationFactory();
        var serverUrl = factory.Server.BaseAddress;
        
        // Use the URL with external HTTP client
        using var externalClient = new HttpClient();
        var response = await externalClient.GetAsync($"{serverUrl}api/tenants");
        
        // This typically won't work because the test server is in-memory
        // You'd need to use CreateClient() from the factory instead
    }
}
```

## Common Patterns

### Pattern 1: Storing URL for Multiple Tests

```csharp
public class TenantApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly Uri _serverUrl;

    public TenantApiTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _serverUrl = factory.Server.BaseAddress;
    }

    [Fact]
    public async Task Test1_UsesServerUrl()
    {
        var endpoint = new Uri(_serverUrl, "api/tenants");
        var response = await _client.GetAsync(endpoint);
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Test2_UsesServerUrl()
    {
        var endpoint = new Uri(_serverUrl, "api/health");
        var response = await _client.GetAsync(endpoint);
        Assert.True(response.IsSuccessStatusCode);
    }
}
```

### Pattern 2: Helper Method in Factory

Add this to your `TestWebApplicationFactory`:

```csharp
/// <summary>
/// Gets the base URL of the test server including scheme, host, and port
/// </summary>
public Uri GetServerUrl()
{
    return Server.BaseAddress;
}

/// <summary>
/// Builds a full URL for a specific endpoint
/// </summary>
public Uri GetEndpointUrl(string relativeUrl)
{
    return new Uri(Server.BaseAddress, relativeUrl);
}
```

Usage:
```csharp
[Fact]
public async Task UseHelperMethods()
{
    var serverUrl = _factory.GetServerUrl();
    var tenantsUrl = _factory.GetEndpointUrl("api/tenants");
    
    Console.WriteLine($"Server: {serverUrl}");
    Console.WriteLine($"Endpoint: {tenantsUrl}");
}
```

## Important Notes

### In-Memory Test Server

By default, `WebApplicationFactory` creates an **in-memory test server**:
- No actual TCP port is opened
- No external clients can connect
- The server is only accessible through the `HttpClient` created by the factory
- `Server.BaseAddress` will show a URL (like `http://localhost/`) but it's not a real network address

### Real Server (If Needed)

If you need a real server with an actual TCP port:

```csharp
public class RealServerFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Create test host
        var testHost = builder.Build();

        // Create real host with Kestrel
        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseKestrel();
            webHostBuilder.UseUrls("http://localhost:0"); // Random port
        });

        var host = builder.Build();
        host.Start();

        return testHost;
    }
}
```

## Summary

**For most integration tests:**
```csharp
// Get URL
var serverUrl = factory.Server.BaseAddress;

// Or from client
var client = factory.CreateClient();
var serverUrl = client.BaseAddress;
```

**For SignalR connections:**
```csharp
var hubUrl = new Uri(factory.Server.BaseAddress, "/hubs/connector");
var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl, o => o.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler())
    .Build();
```

**Key Property:**
- `factory.Server.BaseAddress` - Gets the test server URL with scheme, host, and port
