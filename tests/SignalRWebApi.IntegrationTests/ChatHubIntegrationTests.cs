using Microsoft.AspNetCore.SignalR.Client;
using SignalRWebApi.IntegrationTests.Fixtures;
using Xunit;

namespace SignalRWebApi.IntegrationTests;

/// <summary>
/// Integration tests for SignalR Hub with Redis backplane
/// Demonstrates how to configure and test Web API with SignalR in test project
/// </summary>
public class ChatHubIntegrationTests : IClassFixture<SignalRWebApplicationFactory>, IAsyncLifetime
{
    private readonly SignalRWebApplicationFactory _factory;
    private HubConnection? _connection1;
    private HubConnection? _connection2;

    public ChatHubIntegrationTests(SignalRWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        // Create test HTTP client from the factory
        var httpClient = _factory.CreateClient();

        // Build SignalR client connections using the test server
        _connection1 = new HubConnectionBuilder()
            .WithUrl($"{httpClient.BaseAddress}chathub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        _connection2 = new HubConnectionBuilder()
            .WithUrl($"{httpClient.BaseAddress}chathub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        // Start both connections
        await _connection1.StartAsync();
        await _connection2.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_connection1 != null)
        {
            await _connection1.StopAsync();
            await _connection1.DisposeAsync();
        }

        if (_connection2 != null)
        {
            await _connection2.StopAsync();
            await _connection2.DisposeAsync();
        }
    }

    [Fact]
    public async Task SendMessage_ShouldBroadcastToAllConnectedClients()
    {
        // Arrange
        var tcs = new TaskCompletionSource<(string user, string message)>();
        
        _connection2!.On<string, string>("ReceiveMessage", (user, message) =>
        {
            tcs.SetResult((user, message));
        });

        // Act
        await _connection1!.InvokeAsync("SendMessage", "TestUser", "Hello World");

        // Assert
        var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("TestUser", result.user);
        Assert.Equal("Hello World", result.message);
    }

    [Fact]
    public async Task JoinGroup_ShouldReceiveGroupMessages()
    {
        // Arrange
        var groupName = "TestGroup";
        var tcs = new TaskCompletionSource<(string user, string message)>();

        _connection2!.On<string, string>("ReceiveMessage", (user, message) =>
        {
            tcs.SetResult((user, message));
        });

        // Act - Join group with connection2
        await _connection2.InvokeAsync("JoinGroup", groupName);
        
        // Wait a bit for group join to complete
        await Task.Delay(100);

        // Send message to group from connection1
        await _connection1!.InvokeAsync("SendMessageToGroup", groupName, "TestUser", "Group Message");

        // Assert
        var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("TestUser", result.user);
        Assert.Equal("Group Message", result.message);
    }

    [Fact]
    public async Task LeaveGroup_ShouldNotReceiveGroupMessages()
    {
        // Arrange
        var groupName = "TestGroup2";
        var messageReceived = false;

        _connection2!.On<string, string>("ReceiveMessage", (user, message) =>
        {
            messageReceived = true;
        });

        // Act - Join and then leave group
        await _connection2.InvokeAsync("JoinGroup", groupName);
        await Task.Delay(100);
        await _connection2.InvokeAsync("LeaveGroup", groupName);
        await Task.Delay(100);

        // Send message to group
        await _connection1!.InvokeAsync("SendMessageToGroup", groupName, "TestUser", "Should Not Receive");

        // Wait to ensure message would have been received if still in group
        await Task.Delay(500);

        // Assert
        Assert.False(messageReceived, "Should not receive message after leaving group");
    }

    [Fact]
    public async Task OnConnectedAsync_ShouldNotifyAllClients()
    {
        // Arrange
        var tcs = new TaskCompletionSource<string>();
        
        _connection1!.On<string>("UserConnected", connectionId =>
        {
            tcs.SetResult(connectionId);
        });

        // Act - Create and start a new connection
        var newConnection = new HubConnectionBuilder()
            .WithUrl($"{_factory.CreateClient().BaseAddress}chathub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        await newConnection.StartAsync();

        // Assert
        var connectedId = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(connectedId);
        Assert.NotEmpty(connectedId);

        // Cleanup
        await newConnection.StopAsync();
        await newConnection.DisposeAsync();
    }

    [Fact]
    public async Task Connections_ShouldBeAbleToReconnect()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var connection = new HubConnectionBuilder()
            .WithUrl($"{httpClient.BaseAddress}chathub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .WithAutomaticReconnect()
            .Build();

        // Act & Assert - Start connection
        await connection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, connection.State);

        // Stop and restart
        await connection.StopAsync();
        Assert.Equal(HubConnectionState.Disconnected, connection.State);

        await connection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, connection.State);

        // Cleanup
        await connection.StopAsync();
        await connection.DisposeAsync();
    }
}
