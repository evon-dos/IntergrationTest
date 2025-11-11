using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SignalRWebApi.IntegrationTests.Fixtures;
using Xunit;

namespace SignalRWebApi.IntegrationTests;

/// <summary>
/// Example tests showing how to access configuration from appsettings.IntegrationTests.json
/// </summary>
public class ConfigurationIntegrationTests : IClassFixture<SignalRWebApplicationFactory>
{
    private readonly SignalRWebApplicationFactory _factory;

    public ConfigurationIntegrationTests(SignalRWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Configuration_ShouldLoadIntegrationTestSettings()
    {
        // Arrange - Get configuration from the test host
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();

        // Act - Read values from appsettings.IntegrationTests.json
        var useTestContainers = configuration["IntegrationTest:UseTestContainers"];
        var redisImage = configuration["IntegrationTest:RedisImage"];
        var timeoutSeconds = configuration["IntegrationTest:TimeoutSeconds"];

        // Assert
        Assert.Equal("true", useTestContainers, ignoreCase: true);
        Assert.Equal("redis:7-alpine", redisImage);
        Assert.Equal("30", timeoutSeconds);
    }

    [Fact]
    public void Configuration_ShouldHaveSignalRSettings()
    {
        // Arrange
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();

        // Act - Read SignalR configuration
        var enableDetailedErrors = configuration["SignalR:EnableDetailedErrors"];
        var keepAliveInterval = configuration["SignalR:KeepAliveInterval"];

        // Assert
        Assert.Equal("true", enableDetailedErrors, ignoreCase: true);
        Assert.Equal("00:00:05", keepAliveInterval);
    }

    [Fact]
    public void Configuration_ShouldOverrideRedisConnectionString()
    {
        // Arrange
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();

        // Act - Redis connection string should be overridden by TestContainer
        var redisConnectionString = configuration.GetConnectionString("Redis");

        // Assert - Should not be the value from appsettings.IntegrationTests.json
        Assert.NotNull(redisConnectionString);
        Assert.NotEqual("localhost:6379", redisConnectionString);
        // TestContainer uses 127.0.0.1 with a random port
        Assert.StartsWith("127.0.0.1:", redisConnectionString);
    }

    [Fact]
    public void Configuration_ShouldHaveLoggingSettings()
    {
        // Arrange
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();

        // Act
        var defaultLogLevel = configuration["Logging:LogLevel:Default"];
        var signalRLogLevel = configuration["Logging:LogLevel:Microsoft.AspNetCore.SignalR"];

        // Assert
        Assert.Equal("Information", defaultLogLevel);
        Assert.Equal("Debug", signalRLogLevel);
    }
}
