namespace ECommerceStoreInvoice.Acceptance.Tests;

public class HealthEndpointTests : IClassFixture<ApplicationFactory>
{
    private readonly ApplicationFactory _factory;

    public HealthEndpointTests(ApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}
