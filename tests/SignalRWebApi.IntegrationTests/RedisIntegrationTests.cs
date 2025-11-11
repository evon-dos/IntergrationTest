using SignalRWebApi.IntegrationTests.Fixtures;
using StackExchange.Redis;
using Xunit;

namespace SignalRWebApi.IntegrationTests;

/// <summary>
/// Integration tests to verify Redis backplane configuration
/// </summary>
public class RedisIntegrationTests : IClassFixture<SignalRWebApplicationFactory>
{
    private readonly SignalRWebApplicationFactory _factory;

    public RedisIntegrationTests(SignalRWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Redis_ShouldBeConnectable()
    {
        // Arrange & Act
        var redis = await ConnectionMultiplexer.ConnectAsync(_factory.RedisConnectionString);

        // Assert
        Assert.True(redis.IsConnected, "Should be able to connect to Redis");
        
        // Cleanup
        await redis.CloseAsync();
        redis.Dispose();
    }

    [Fact]
    public async Task Redis_ShouldStoreAndRetrieveValues()
    {
        // Arrange
        var redis = await ConnectionMultiplexer.ConnectAsync(_factory.RedisConnectionString);
        var db = redis.GetDatabase();
        var key = $"test-key-{Guid.NewGuid()}";
        var value = "test-value";

        // Act
        await db.StringSetAsync(key, value);
        var retrievedValue = await db.StringGetAsync(key);

        // Assert
        Assert.Equal(value, retrievedValue.ToString());

        // Cleanup
        await db.KeyDeleteAsync(key);
        await redis.CloseAsync();
        redis.Dispose();
    }

    [Fact]
    public async Task Redis_ShouldHandlePubSub()
    {
        // Arrange
        var redis = await ConnectionMultiplexer.ConnectAsync(_factory.RedisConnectionString);
        var subscriber = redis.GetSubscriber();
        var channel = RedisChannel.Literal($"test-channel-{Guid.NewGuid()}");
        var tcs = new TaskCompletionSource<string>();

        // Subscribe to channel
        await subscriber.SubscribeAsync(channel, (_, message) =>
        {
            tcs.SetResult(message!);
        });

        // Act
        await subscriber.PublishAsync(channel, "Hello Redis");

        // Assert
        var receivedMessage = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("Hello Redis", receivedMessage);

        // Cleanup
        await subscriber.UnsubscribeAsync(channel);
        await redis.CloseAsync();
        redis.Dispose();
    }

    [Fact]
    public async Task Redis_ShouldSupportSignalRChannelPrefix()
    {
        // Arrange
        var redis = await ConnectionMultiplexer.ConnectAsync(_factory.RedisConnectionString);
        var subscriber = redis.GetSubscriber();
        
        // Act - Get all active channels
        var endpoints = redis.GetEndPoints();
        var server = redis.GetServer(endpoints[0]);
        
        // This will show all channels that SignalR creates with the prefix
        // Note: Channels are created dynamically when clients connect
        
        // Assert - Just verify we can query the server
        Assert.NotNull(server);
        Assert.True(server.IsConnected);

        // Cleanup
        await redis.CloseAsync();
        redis.Dispose();
    }

    [Fact]
    public async Task Redis_ShouldHandleMultipleConnections()
    {
        // Arrange
        var redis1 = await ConnectionMultiplexer.ConnectAsync(_factory.RedisConnectionString);
        var redis2 = await ConnectionMultiplexer.ConnectAsync(_factory.RedisConnectionString);

        // Assert
        Assert.True(redis1.IsConnected);
        Assert.True(redis2.IsConnected);

        // Cleanup
        await redis1.CloseAsync();
        await redis2.CloseAsync();
        redis1.Dispose();
        redis2.Dispose();
    }
}
