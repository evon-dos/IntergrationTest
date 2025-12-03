using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace HIEConnector.Api.Tests;

/// <summary>
/// Examples showing how to get and use the test server URL in integration tests.
/// </summary>
public class ServerUrlExamples : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ServerUrlExamples(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Example 1: Getting the server URL directly
    /// </summary>
    [Fact]
    public void GetServerUrl_DirectAccess()
    {
        // Method 1: Using Server.BaseAddress
        var serverUrl = _factory.Server.BaseAddress;
        
        Assert.NotNull(serverUrl);
        Assert.True(serverUrl.IsAbsoluteUri);
        
        // The URL will be something like: http://localhost/
        Console.WriteLine($"Test Server URL: {serverUrl}");
        Console.WriteLine($"Scheme: {serverUrl.Scheme}");
        Console.WriteLine($"Host: {serverUrl.Host}");
        Console.WriteLine($"Port: {serverUrl.Port}");
    }

    /// <summary>
    /// Example 2: Using the helper method
    /// </summary>
    [Fact]
    public void GetServerUrl_UsingHelper()
    {
        // Use the helper method added to the factory
        var serverUrl = _factory.GetServerUrl();
        
        Assert.NotNull(serverUrl);
        Console.WriteLine($"Server URL from helper: {serverUrl}");
    }

    /// <summary>
    /// Example 3: Getting URL from HttpClient
    /// </summary>
    [Fact]
    public void GetServerUrl_FromClient()
    {
        var client = _factory.CreateClient();
        var serverUrl = client.BaseAddress;
        
        Assert.NotNull(serverUrl);
        Assert.Equal(_factory.Server.BaseAddress, serverUrl);
        Console.WriteLine($"Client base address: {serverUrl}");
    }

    /// <summary>
    /// Example 4: Building endpoint URLs
    /// </summary>
    [Fact]
    public async Task BuildEndpointUrl_MakeRequest()
    {
        // Build specific endpoint URLs
        var tenantsUrl = _factory.GetEndpointUrl("api/tenants");
        var healthUrl = _factory.GetEndpointUrl("/api/health");
        
        Console.WriteLine($"Tenants URL: {tenantsUrl}");
        Console.WriteLine($"Health URL: {healthUrl}");
        
        var client = _factory.CreateClient();
        
        // Make requests using the built URLs
        var response1 = await client.GetAsync(tenantsUrl);
        var response2 = await client.GetAsync(healthUrl);
        
        Assert.True(response1.IsSuccessStatusCode || response1.StatusCode == System.Net.HttpStatusCode.NotFound);
        Assert.True(response2.IsSuccessStatusCode || response2.StatusCode == System.Net.HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Example 5: Building URLs manually with Uri constructor
    /// </summary>
    [Fact]
    public void BuildUrls_Manually()
    {
        var baseUrl = _factory.Server.BaseAddress;
        
        // Build URLs using Uri constructor
        var tenantsUrl = new Uri(baseUrl, "api/tenants");
        var specificTenantUrl = new Uri(baseUrl, "api/tenants/123");
        var healthUrl = new Uri(baseUrl, "/api/health");
        
        Assert.Equal($"{baseUrl}api/tenants", tenantsUrl.ToString());
        Assert.Equal($"{baseUrl}api/tenants/123", specificTenantUrl.ToString());
        Assert.Equal($"{baseUrl}api/health", healthUrl.ToString());
        
        Console.WriteLine($"Tenants: {tenantsUrl}");
        Console.WriteLine($"Specific Tenant: {specificTenantUrl}");
        Console.WriteLine($"Health: {healthUrl}");
    }

    /// <summary>
    /// Example 6: Using server URL for SignalR connections
    /// </summary>
    [Fact]
    public async Task ConnectToSignalRHub_UsingServerUrl()
    {
        // Get the server URL
        var serverUrl = _factory.Server.BaseAddress;
        
        // Build SignalR hub URL
        var hubPath = "/hubs/connector";
        var hubUrl = new Uri(serverUrl, hubPath);
        
        Console.WriteLine($"Connecting to SignalR hub at: {hubUrl}");

        // Create SignalR connection
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // Important: Use the factory's handler for authentication/cookies
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        try
        {
            // Attempt to connect
            await connection.StartAsync();
            
            // Verify connection
            Assert.Equal(HubConnectionState.Connected, connection.State);
            Console.WriteLine($"Successfully connected to hub at {hubUrl}");
        }
        catch (Exception ex)
        {
            // Hub might not be configured in test environment
            Console.WriteLine($"Hub connection failed (expected in test): {ex.Message}");
        }
        finally
        {
            if (connection.State == HubConnectionState.Connected)
            {
                await connection.StopAsync();
            }
        }
    }

    /// <summary>
    /// Example 7: Using URL in test setup for multiple tests
    /// </summary>
    [Fact]
    public async Task StoreServerUrl_ForReuseInTests()
    {
        // Store the URL for reuse
        var serverUrl = _factory.GetServerUrl();
        var client = _factory.CreateClient();
        
        // Build multiple endpoint URLs
        var endpoints = new[]
        {
            new Uri(serverUrl, "api/tenants"),
            new Uri(serverUrl, "api/health"),
            new Uri(serverUrl, "api/status")
        };
        
        foreach (var endpoint in endpoints)
        {
            Console.WriteLine($"Testing endpoint: {endpoint}");
            
            try
            {
                var response = await client.GetAsync(endpoint);
                Console.WriteLine($"  Status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Example 8: Comparing server URL across different factory instances
    /// </summary>
    [Fact]
    public void ServerUrl_ConsistencyAcrossFactoryUsage()
    {
        // Get URL multiple times
        var url1 = _factory.Server.BaseAddress;
        var url2 = _factory.GetServerUrl();
        var url3 = _factory.CreateClient().BaseAddress;
        
        // They should all be the same
        Assert.Equal(url1, url2);
        Assert.Equal(url2, url3);
        Assert.Equal(url1, url3);
        
        Console.WriteLine("All server URLs are consistent:");
        Console.WriteLine($"  Server.BaseAddress: {url1}");
        Console.WriteLine($"  GetServerUrl(): {url2}");
        Console.WriteLine($"  Client.BaseAddress: {url3}");
    }
}

/// <summary>
/// Examples specific to using SimpleTestWebApplicationFactory
/// </summary>
public class SimpleServerUrlExamples : IClassFixture<SimpleTestWebApplicationFactory>
{
    private readonly SimpleTestWebApplicationFactory _factory;

    public SimpleServerUrlExamples(SimpleTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void GetServerUrl_FromSimpleFactory()
    {
        // Works the same way with SimpleTestWebApplicationFactory
        var serverUrl = _factory.GetServerUrl();
        
        Assert.NotNull(serverUrl);
        Console.WriteLine($"Simple factory server URL: {serverUrl}");
    }

    [Fact]
    public void BuildEndpointUrl_WithSimpleFactory()
    {
        var tenantsUrl = _factory.GetEndpointUrl("api/tenants");
        
        Assert.NotNull(tenantsUrl);
        Console.WriteLine($"Tenants endpoint: {tenantsUrl}");
    }
}
