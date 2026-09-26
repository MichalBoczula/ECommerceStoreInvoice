using System.Net;
using System.Text;
using System.Text.Json;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests;

public class RequestBodyErrorTests : IClassFixture<ApplicationFactory>
{
    private readonly ApplicationFactory _factory;

    public RequestBodyErrorTests(ApplicationFactory factory) => _factory = factory;

    [Theory]
    [InlineData("{ invalid json")]
    [InlineData("{}")]
    public async Task UpdateShoppingCart_WithInvalidJson_ShouldReturnSafeProblem(string body)
    {
        using var client = _factory.CreateClient();
        var path = $"/shopping-carts/{Guid.NewGuid()}";
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.PutAsync(path, content);

        await AssertBadRequest(response, path);
    }

    [Fact]
    public async Task UpdateShoppingCart_WithMissingBody_ShouldReturnSafeProblem()
    {
        using var client = _factory.CreateClient();
        var path = $"/shopping-carts/{Guid.NewGuid()}";
        using var request = new HttpRequestMessage(HttpMethod.Put, path);

        using var response = await client.SendAsync(request);

        await AssertBadRequest(response, path);
    }

    private static async Task AssertBadRequest(HttpResponseMessage response, string path)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        root.GetProperty("status").GetInt32().ShouldBe(400);
        root.GetProperty("title").GetString().ShouldBe("Invalid JSON payload.");
        root.GetProperty("detail").GetString().ShouldBe("The request body must contain valid JSON.");
        root.GetProperty("instance").GetString().ShouldBe(path);
        root.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain("JsonException");
        body.ShouldNotContain("BytePositionInLine");
        body.ShouldNotContain("stackTrace");
    }
}
