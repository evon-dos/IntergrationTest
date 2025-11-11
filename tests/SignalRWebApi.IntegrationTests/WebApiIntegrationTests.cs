using System.Net;
using SignalRWebApi.IntegrationTests.Fixtures;
using Xunit;

namespace SignalRWebApi.IntegrationTests;

/// <summary>
/// Integration tests for Web API endpoints
/// </summary>
public class WebApiIntegrationTests : IClassFixture<SignalRWebApplicationFactory>
{
    private readonly SignalRWebApplicationFactory _factory;

    public WebApiIntegrationTests(SignalRWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WeatherForecast_ShouldReturnSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/weatherforecast");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task ChatHub_ShouldBeAccessible()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Try to negotiate with SignalR hub
        var response = await client.PostAsync("/chathub/negotiate", null);

        // Assert - Should return OK or specific SignalR response
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
