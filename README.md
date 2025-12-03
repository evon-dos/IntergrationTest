# IntergrationTest

This repository demonstrates integration testing in ASP.NET Core using `WebApplicationFactory<Program>` with the `IClassFixture` pattern.

## Project Structure

- **WebApp**: ASP.NET Core web application with a simple "Hello World!" endpoint
- **WebApp.IntegrationTests**: Integration test project using xUnit and WebApplicationFactory

## Key Features

The solution demonstrates:
- Using `IClassFixture<WebApplicationFactory<Program>>` for integration testing
- Testing without manually calling `WebApplication.CreateBuilder()`
- Exposing the implicit `Program` class for testing using `public partial class Program { }`

## Running Tests

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal
```

## Integration Test Example

The `BasicIntegrationTests` class shows how to:
1. Inject `WebApplicationFactory<Program>` via constructor
2. Create HTTP clients to test endpoints
3. Verify responses and content

```csharp
public class BasicIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BasicIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_EndpointReturnsSuccessAndCorrectContentType()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/");
        
        response.EnsureSuccessStatusCode();
    }
}
```