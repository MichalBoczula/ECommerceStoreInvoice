using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using ECommerceStoreInvoice.ExternalProviders.IntegrationTests.Configuration;
using PactNet;
using Shouldly;

namespace ECommerceStoreInvoice.ExternalProviders.IntegrationTests.Tests;

public sealed class ProductsConsumerPactTests
{
    private const string PactFile = "invoice-products.json";
    private const string PathByIds = "/mobile-phones/by-ids";
    private const string Json = "application/json";
    private const string ProblemJson = "application/problem+json";
    private const string ProductName = "Pact checkout phone";
    private const string Brand = "Pact";
    private const string Currency = "PLN";
    private const decimal Amount = 2499.50m;
    private static readonly Guid ProductId = Guid.Parse("42000000-0000-4000-8000-000000000001");
    private static readonly Guid MissingId = Guid.Parse("42000000-0000-4000-8000-000000000099");

    [Fact]
    public async Task CheckoutProductLookupPublishesReproducibleConsumerPact()
    {
        Environment.SetEnvironmentVariable("PACT_DO_NOT_TRACK", "true");
        var workspace = Directory.CreateTempSubdirectory("invoice-products-pact-");
        try
        {
            var first = await VerifyAsync(workspace.CreateSubdirectory("first").FullName);
            var second = await VerifyAsync(workspace.CreateSubdirectory("second").FullName);
            first.AsSpan().SequenceEqual(second).ShouldBeTrue("fresh Pact servers must produce the same bytes");

            using var document = JsonDocument.Parse(first);
            var pact = document.RootElement;
            pact.GetProperty("consumer").GetProperty("name").GetString().ShouldBe("invoice");
            pact.GetProperty("provider").GetProperty("name").GetString().ShouldBe("products");
            pact.GetProperty("interactions").GetArrayLength().ShouldBe(2);
            pact.GetProperty("metadata").GetProperty("pactSpecification").GetProperty("version").GetString().ShouldBe("4.0");

            var output = Environment.GetEnvironmentVariable("INVOICE_PACT_DIR");
            if (!string.IsNullOrWhiteSpace(output))
            {
                Directory.CreateDirectory(output);
                await File.WriteAllBytesAsync(Path.Combine(output, PactFile), first);
                await File.WriteAllTextAsync(Path.Combine(output, "manifest.json"), JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    consumer = "invoice",
                    provider = "products",
                    pactSpecification = "4.0",
                    pactNet = "5.0.1",
                    file = PactFile,
                    sha256 = Convert.ToHexStringLower(SHA256.HashData(first)),
                    interactionCount = 2
                }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            }
        }
        finally
        {
            workspace.Delete(recursive: true);
        }
    }

    private static async Task<byte[]> VerifyAsync(string directory)
    {
        var pact = Pact.V4("invoice", "products", new PactConfig { PactDir = directory }).WithHttpInteractions();

        pact.UponReceiving("look up a priced phone when another requested phone is missing")
            .Given("one priced phone exists and the other requested phone is missing", new Dictionary<string, string>
            {
                ["productId"] = ProductId.ToString(),
                ["missingId"] = MissingId.ToString(),
                ["name"] = ProductName,
                ["brand"] = Brand,
                ["amount"] = "2499.50",
                ["currency"] = Currency
            })
            .WithRequest(HttpMethod.Post, PathByIds)
            .WithHeader("Content-Type", Json)
            .WithJsonBody(new[] { ProductId, MissingId })
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", Json)
            .WithJsonBody(new[] { new { id = ProductId, name = ProductName, brand = Brand, price = new { amount = Amount, currency = Currency } } });

        pact.UponReceiving("look up a phone that does not exist")
            .Given("the requested phone does not exist", new Dictionary<string, string> { ["missingId"] = MissingId.ToString() })
            .WithRequest(HttpMethod.Post, PathByIds)
            .WithHeader("Content-Type", Json)
            .WithJsonBody(new[] { MissingId })
            .WillRespond()
            .WithStatus(HttpStatusCode.NotFound)
            .WithHeader("Content-Type", ProblemJson)
            .WithBody("""{"status":404}""", ProblemJson);

        await pact.VerifyAsync(async context =>
        {
            using var http = new HttpClient { BaseAddress = context.MockServerUri };
            var client = ExternalProvidersApplicationFactory.CreateClient(http);
            var products = await client.GetProductsByIds([ProductId, MissingId, ProductId]);
            var snapshot = products.ShouldHaveSingleItem();
            snapshot.ProductId.ShouldBe(ProductId);
            snapshot.Name.ShouldBe(ProductName);
            snapshot.Brand.ShouldBe(Brand);
            snapshot.Price.Amount.ShouldBe(Amount);
            snapshot.Price.Currency.ShouldBe(Currency);
            (await client.GetProductsByIds([MissingId])).ShouldBeEmpty();
        });
        return await File.ReadAllBytesAsync(Path.Combine(directory, PactFile));
    }
}
