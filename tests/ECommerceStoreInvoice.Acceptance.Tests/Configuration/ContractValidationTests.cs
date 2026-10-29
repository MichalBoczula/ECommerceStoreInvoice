using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Infrastructure.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceStoreInvoice.Acceptance.Tests.Configuration;

public sealed class ContractDocumentFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    public JsonDocument Document { get; private set; } = null!;
    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("OpenApiExport");
            builder.UseSetting("MongoDbSettings:ConnectionString", "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500");
            builder.ConfigureTestServices(services => services.AddScoped<MongoInitializer>(_ => throw new InvalidOperationException("Contract export must not initialize Mongo.")));
        });
        using var client = _factory.CreateClient();
        Document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        _ = OpenApiContractAssertions.Check(Document);
    }
    public async Task DisposeAsync() { Document.Dispose(); if (_factory is not null) await _factory.DisposeAsync(); }
}

public sealed class ContractValidationTests(ContractDocumentFixture fixture) : IClassFixture<ContractDocumentFixture>
{
    [Theory]
    [InlineData("nullability")]
    [InlineData("response-required")]
    [InlineData("request-required")]
    [InlineData("scalar")]
    [InlineData("nested")]
    [InlineData("format")]
    [InlineData("example")]
    [InlineData("description")]
    [InlineData("request-extra")]
    [InlineData("problem-required")]
    [InlineData("body-alternatives")]
    [InlineData("pdf")]
    [InlineData("pdf-headers")]
    public void PublishedMetadataDriftIsRejected(string mutation)
    {
        var root = JsonNode.Parse(fixture.Document.RootElement.GetRawText())!;
        var schemas = root["components"]!["schemas"]!;
        var order = schemas["OrderResponseDto"]!;
        var client = schemas["CreateClientDataVersionRequestDto"]!;
        var pdf = root["paths"]!["/invoices/{invoiceId}/pdf"]!["get"]!["responses"]!["200"]!;
        switch (mutation)
        {
            case "nullability": client["properties"]!["clientName"]!["nullable"] = false; break;
            case "response-required": Remove(schemas["InvoiceResponseDto"]!["required"]!, "id"); break;
            case "request-required": Remove(client["required"]!, "apartmentNumber"); break;
            case "scalar": order["properties"]!["totalAmount"]!["type"] = "string"; break;
            case "nested": order["properties"]!["lines"]!["items"] = new JsonObject { ["$ref"] = "#/components/schemas/ProductVersionResponseDto" }; break;
            case "format": client["properties"]!["phoneNumber"]!["format"] = "uuid"; break;
            case "example": schemas["ShoppingCartLineResponseDto"]!["properties"]!["quantity"]!["example"] = "two"; break;
            case "description": schemas["InvoiceResponseDto"]!["properties"]!["storageUrl"]!.AsObject().Remove("description"); break;
            case "request-extra": schemas["UpdateShoppingCartRequestDto"]!["additionalProperties"] = false; break;
            case "problem-required": Remove(schemas["ProblemDetails"]!["required"]!, "traceId"); break;
            case "body-alternatives": root["paths"]!["/orders/{orderId}/status"]!["patch"]!["responses"]!["400"]!["content"]!["application/problem+json"]!["schema"]!["oneOf"]!.AsArray().RemoveAt(0); break;
            case "pdf": pdf["content"]!["application/pdf"]!["schema"]!["format"] = "byte"; break;
            case "pdf-headers": pdf["headers"]!.AsObject().Remove("Cache-Control"); break;
            default: throw new InvalidOperationException(mutation);
        }
        using var mutated = JsonDocument.Parse(root.ToJsonString());
        var error = Record.Exception(() => OpenApiContractAssertions.Check(mutated));
        Assert.True(error is InvalidOperationException or KeyNotFoundException, error?.ToString() ?? "Contract drift was accepted.");
    }

    [Theory]
    [InlineData("uuid")]
    [InlineData("null-client")]
    [InlineData("money-string")]
    [InlineData("fractional-quantity")]
    [InlineData("boolean-string")]
    [InlineData("date-only")]
    [InlineData("nested-money")]
    [InlineData("missing-status")]
    [InlineData("null-name")]
    [InlineData("extra-member")]
    [InlineData("dictionary")]
    [InlineData("problem")]
    public async Task ActualResponseGuardRejectsScalarNullNestedAndCompleteProblemDrift(string mutation)
    {
        var schemas = fixture.Document.RootElement.GetProperty("components").GetProperty("schemas");
        var type = mutation == "dictionary" ? "FlowDescriptorsResponseDto" : mutation == "problem" ? "NotFoundProblemDetails" : "OrderResponseDto";
        var body = JsonNode.Parse(schemas.GetProperty(type).GetProperty("example").GetRawText())!;
        var snapshot = mutation is "dictionary" or "problem" ? null : body["lines"]![0]!["productVersion"];
        switch (mutation)
        {
            case "uuid": body["id"] = "not-a-uuid"; break;
            case "null-client": body["clientId"] = null; break;
            case "money-string": body["totalAmount"] = "25.00"; break;
            case "fractional-quantity": body["lines"]![0]!["quantity"] = 1.5; break;
            case "boolean-string": snapshot!["isActive"] = "true"; break;
            case "date-only": snapshot!["createdAt"] = "2024-01-01"; break;
            case "nested-money": snapshot!["priceAmount"] = "12.50"; break;
            case "missing-status": body.AsObject().Remove("status"); break;
            case "null-name": snapshot!["name"] = null; break;
            case "extra-member": body["internalException"] = "private-marker"; break;
            case "dictionary": body["flows"]![0]!.AsObject().First().Value!["steps"]![0]!["order"] = "one"; break;
            case "problem": body["traceId"] = null; break;
            default: throw new InvalidOperationException(mutation);
        }
        var problem = mutation == "problem";
        using var client = new HttpClient(new OpenApiResponseHandler(fixture.Document)
        {
            InnerHandler = new Stub(() => new HttpResponseMessage(problem ? HttpStatusCode.NotFound : HttpStatusCode.OK)
            { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, problem ? "application/problem+json" : "application/json") })
        });
        var path = mutation == "dictionary" ? "/orders-documentation/flows" : problem ? "/invoices/11111111-1111-4111-8111-111111111111" : "/orders/11111111-1111-4111-8111-111111111111";
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("http://localhost" + path));
        Assert.Contains("Response contract mismatch", error.Message);
    }

    [Fact]
    public void ExplicitNullableMembersRemainRequiredAndAcceptNullInCompleteResponses()
    {
        var schemas = fixture.Document.RootElement.GetProperty("components").GetProperty("schemas");
        foreach (var type in new[] { "ClientDataVersionResponseDto", "OrderResponseDto" })
        {
            var schema = schemas.GetProperty(type);
            var body = JsonNode.Parse(schema.GetProperty("example").GetRawText())!;
            var fields = type.StartsWith("Client", StringComparison.Ordinal) ? new[] { "clientName", "city", "street", "buildingNumber", "apartmentNumber" } : new[] { "updatedAt" };
            foreach (var field in fields) body[field] = null;
            if (type == "OrderResponseDto") body["lines"]![0]!["productVersion"]!["deactivatedAt"] = null;
            using var value = JsonDocument.Parse(body.ToJsonString());
            Assert.Empty(JsonContractValidator.Check(schema, value.RootElement, schemas));
            foreach (var field in fields)
            {
                Assert.Contains(schema.GetProperty("required").EnumerateArray(), item => item.GetString() == field);
                body.AsObject().Remove(field);
                using var missing = JsonDocument.Parse(body.ToJsonString());
                Assert.NotEmpty(JsonContractValidator.Check(schema, missing.RootElement, schemas));
                body[field] = null;
            }
        }
    }

    private static void Remove(JsonNode array, string field) => array.AsArray().Remove(array.AsArray().Single(item => item!.GetValue<string>() == field));
    private sealed class Stub(Func<HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response());
    }
}
