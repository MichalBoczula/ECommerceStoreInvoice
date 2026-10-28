using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using MongoDB.Bson;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ShoppingCarts;

/// <summary>Scenario-local fixtures and complete HTTP/MongoDB assertions for cart successes.</summary>
public sealed class ShoppingCartSuccessState(ScenarioApiContext context)
{
    private BsonDocument[]? _before;
    private ShoppingCartResponseDto? _response;
    private string _operation = string.Empty;
    private DateTime _started;
    private DateTime _completed;
    internal Guid ClientId { get; set; } = Guid.NewGuid();
    internal Guid OtherClientId { get; } = Guid.NewGuid();
    internal ShoppingCartLineResponseDto[] RequestLines { get; set; } = [];

    internal static Dictionary<string, string> Values(Table table) => table.Rows.ToDictionary(
        row => row["Field"], row => row["Value"], StringComparer.Ordinal);

    internal static string Required(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) ? value
            : throw new InvalidOperationException($"Missing explicit cart fixture/expectation: {key}");

    internal static int Number(IReadOnlyDictionary<string, string> values, string key) =>
        int.Parse(Required(values, key), CultureInfo.InvariantCulture);

    internal static ShoppingCartLineResponseDto[] Lines(IReadOnlyDictionary<string, string> values, int count) =>
        Enumerable.Range(1, count).Select(index => new ShoppingCartLineResponseDto
        {
            ProductId = Guid.Parse(Required(values, $"Line{index}ProductId")),
            Quantity = Number(values, $"Line{index}Quantity")
        }).ToArray();

    internal async Task CreateEmptyFixtureAsync(string title)
    {
        context.RecordEvidence(title + " request", new { ClientId });
        var started = DateTime.UtcNow;
        using var response = await context.HttpClient.PostAsync($"/shopping-carts/{ClientId}", null);
        var completed = DateTime.UtcNow;
        AllureJson.AttachRawJson($"Setup response JSON ({(int)response.StatusCode})", await response.Content.ReadAsStringAsync());
        var actual = await ReadResponseAsync(response);
        context.RecordEvidence(title + " response", actual);
        actual.ClientId.ShouldBe(ClientId);
        actual.Lines.ShouldBeEmpty();
        InWindow(actual.CreatedAt, started, completed);
        InWindow(actual.UpdatedAt, started, completed);
        var documents = await LoadAsync();
        AttachDocuments(title + " database", documents);
        EqualCart(Cart(documents.Single(document => Client(document) == ClientId)), actual);
    }

    internal async Task SeedAsync(Table table, bool other)
    {
        var values = Values(table);
        Required(values, "Id").ShouldBe("generated");
        var cart = new ShoppingCartResponseDto
        {
            Id = Guid.NewGuid(),
            ClientId = other ? OtherClientId : ClientId,
            CreatedAt = DateTime.Parse(Required(values, "CreatedAt"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            UpdatedAt = DateTime.Parse(Required(values, "UpdatedAt"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Lines = Lines(values, Number(values, "LinesCount"))
        };
        context.RecordEvidence("Stored cart fixture expectations", new { client = other ? "other" : "target", values, cart });
        cart.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        cart.UpdatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        var document = new BsonDocument
        {
            ["_id"] = new BsonBinaryData(cart.Id, GuidRepresentation.Standard),
            ["ClientId"] = new BsonBinaryData(cart.ClientId, GuidRepresentation.Standard),
            ["CreatedAt"] = cart.CreatedAt,
            ["UpdatedAt"] = cart.UpdatedAt,
            ["Lines"] = new BsonArray(cart.Lines.Select(line => new BsonDocument
            {
                ["ProductId"] = new BsonBinaryData(line.ProductId, GuidRepresentation.Standard),
                ["Quantity"] = line.Quantity
            }))
        };
        await context.Factory.SeedShoppingCartDocumentAsync(document);
        var documents = await LoadAsync();
        AttachDocuments("Stored cart fixture database", documents);
        EqualCart(Cart(documents.Single(item => Client(item) == cart.ClientId)), cart);
    }

    internal async Task SendAsync(string operation)
    {
        _operation = operation;
        _before = await LoadAsync();
        AttachDocuments("Shopping cart database before", _before);
        _started = DateTime.UtcNow;
        context.Response = operation switch
        {
            "Create" => await context.HttpClient.PostAsync($"/shopping-carts/{ClientId}", null),
            "Get" => await context.HttpClient.GetAsync($"/shopping-carts/client/{ClientId}"),
            "Update" => await context.HttpClient.PutAsJsonAsync($"/shopping-carts/{ClientId}", new UpdateShoppingCartRequestDto
            {
                Lines = RequestLines.Select(line => new ShoppingCartLineRequestDto { ProductId = line.ProductId, Quantity = line.Quantity }).ToArray()
            }, context.JsonOptions),
            _ => throw new InvalidOperationException("Unknown cart operation.")
        };
        _completed = DateTime.UtcNow;
        context.RecordEvidence("Shopping cart request window", new { operation, startedAt = _started, completedAt = _completed });
        AllureJson.AttachRawJson($"Response JSON ({(int)context.Response.StatusCode})", await context.Response.Content.ReadAsStringAsync());
    }

    internal async Task ExpectResponseAsync(Table table)
    {
        var expected = Values(table);
        context.RecordEvidence("Shopping cart response expectations", expected);
        foreach (var key in new[] { "HasId", "HasClientId", "HasCreatedAt", "HasUpdatedAt" })
            Required(expected, key).ShouldBe("true");
        context.Response.ShouldNotBeNull();
        ((int)context.Response.StatusCode).ShouldBe(Number(expected, "StatusCode"));
        _response = await ReadResponseAsync(context.Response);
        context.RecordEvidence("Shopping cart complete response", _response);
        _response.ClientId.ShouldBe(ClientId);
        _response.Lines.ShouldBe(Lines(expected, Number(expected, "LinesCount")), ignoreOrder: true);
        _response.UpdatedAt.ShouldBeGreaterThanOrEqualTo(_response.CreatedAt);
        _before.ShouldNotBeNull();
        if (_operation == "Create")
        {
            _before.ShouldNotContain(document => Cart(document).Id == _response.Id);
            InWindow(_response.CreatedAt, _started, _completed);
            InWindow(_response.UpdatedAt, _started, _completed);
        }
        else
        {
            var previous = Cart(_before.Single(document => Client(document) == ClientId));
            _response.Id.ShouldBe(previous.Id);
            _response.CreatedAt.ShouldBe(previous.CreatedAt);
            if (_operation == "Get") EqualCart(_response, previous);
            else InWindow(_response.UpdatedAt, _started, _completed);
        }
    }

    internal void ExpectPayload(Table table)
    {
        var expected = Values(table);
        context.RecordEvidence("Shopping cart payload expectations", expected);
        Required(expected, "Id").ShouldBe("<generated guid>");
        Required(expected, "ClientId").ShouldBe("<existing shopping cart client id>");
        Required(expected, "CreatedAt").ShouldBe("<generated timestamp>");
        Required(expected, "UpdatedAt").ShouldBe("<generated timestamp>");
        _response.ShouldNotBeNull();
        _response.Lines.ShouldBe(JsonSerializer.Deserialize<ShoppingCartLineResponseDto[]>(Required(expected, "Lines"), context.JsonOptions)!, ignoreOrder: true);
        AllureJson.AttachObject("Get shopping cart response object", _response, context.JsonOptions);
    }

    internal async Task ExpectDatabaseAsync(Table table)
    {
        var expected = Values(table);
        context.RecordEvidence("Shopping cart persistence expectations", expected);
        var after = await LoadAsync();
        AttachDocuments("Shopping cart database after", after);
        _before.ShouldNotBeNull();
        _response.ShouldNotBeNull();
        _before.Length.ShouldBe(Number(expected, "CartsBefore"));
        after.Length.ShouldBe(Number(expected, "CartsAfter"));
        _before.Count(document => Client(document) == ClientId).ShouldBe(Number(expected, "TargetBefore"));
        after.Count(document => Client(document) == ClientId).ShouldBe(Number(expected, "TargetAfter"));
        var current = Cart(after.Single(document => Client(document) == ClientId));
        EqualCart(current, _response);
        if (_operation == "Create")
        {
            Required(expected, "Id").ShouldBe("generated");
            Required(expected, "CreatedAt").ShouldBe("generated");
            Required(expected, "UpdatedAt").ShouldBe("generated");
            InWindow(current.CreatedAt, Milliseconds(_started), Milliseconds(_completed));
            InWindow(current.UpdatedAt, Milliseconds(_started), Milliseconds(_completed));
        }
        else
        {
            Required(expected, "Id").ShouldBe("retained");
            Required(expected, "CreatedAt").ShouldBe("retained");
            var previous = Cart(_before.Single(document => Client(document) == ClientId));
            current.Id.ShouldBe(previous.Id);
            current.CreatedAt.ShouldBe(previous.CreatedAt);
            if (_operation == "Get")
            {
                Required(expected, "UpdatedAt").ShouldBe("retained");
                after.ShouldBe(_before);
            }
            else
            {
                Required(expected, "UpdatedAt").ShouldBe("advanced");
                current.UpdatedAt.ShouldBeGreaterThan(previous.UpdatedAt);
                InWindow(current.UpdatedAt, Milliseconds(_started), Milliseconds(_completed));
                current.Lines.ShouldBe(RequestLines, ignoreOrder: true);
            }
        }
        Required(expected, "OtherClients").ShouldBe("unchanged");
        var othersBefore = _before.Where(document => Client(document) != ClientId).ToArray();
        var othersAfter = after.Where(document => Client(document) != ClientId).ToArray();
        othersBefore.Length.ShouldBe(Number(expected, "OtherCarts"));
        othersBefore.Length.ShouldBeGreaterThan(0);
        othersAfter.ShouldBe(othersBefore);

        // Supporting reads also pass through the shared HTTP recorder and must not mutate storage.
        foreach (var document in after)
        {
            var stored = Cart(document);
            using var response = await context.HttpClient.GetAsync($"/shopping-carts/client/{stored.ClientId}");
            EqualCart(await ReadResponseAsync(response), stored);
        }
        var afterReads = await LoadAsync();
        AttachDocuments("Shopping cart database after supporting reads", afterReads);
        afterReads.ShouldBe(after);
        context.RecordEvidence("Shopping cart persistence verified", new
        {
            operation = _operation,
            current.Id,
            current.ClientId,
            cartsBefore = _before.Length,
            cartsAfter = after.Length,
            current.Lines,
            current.CreatedAt,
            current.UpdatedAt,
            otherClientsPreserved = true,
            supportingReadsUnchanged = true
        });
    }

    private async Task<BsonDocument[]> LoadAsync() => (await context.Factory.GetShoppingCartDocumentsAsync())
        .OrderBy(document => document["_id"].AsBsonBinaryData.ToGuid()).ToArray();

    private void AttachDocuments(string title, BsonDocument[] documents) => context.RecordEvidence(title,
        JsonSerializer.Deserialize<JsonElement>(new BsonArray(documents).ToJson()));

    private static Guid Client(BsonDocument document) => document["ClientId"].AsBsonBinaryData.ToGuid();

    private static ShoppingCartResponseDto Cart(BsonDocument document)
    {
        document.Names.Order().ShouldBe(new[] { "_id", "ClientId", "CreatedAt", "UpdatedAt", "Lines" }.Order());
        return new ShoppingCartResponseDto
        {
            Id = document["_id"].AsBsonBinaryData.ToGuid(),
            ClientId = Client(document),
            CreatedAt = document["CreatedAt"].ToUniversalTime(),
            UpdatedAt = document["UpdatedAt"].ToUniversalTime(),
            Lines = document["Lines"].AsBsonArray.Select(value =>
            {
                var line = value.AsBsonDocument;
                line.Names.Order().ShouldBe(new[] { "ProductId", "Quantity" }.Order());
                return new ShoppingCartLineResponseDto { ProductId = line["ProductId"].AsBsonBinaryData.ToGuid(), Quantity = line["Quantity"].AsInt32 };
            }).ToArray()
        };
    }

    private async Task<ShoppingCartResponseDto> ReadResponseAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        var cart = await response.Content.ReadFromJsonAsync<ShoppingCartResponseDto>(context.JsonOptions);
        cart.ShouldNotBeNull();
        cart.Id.ShouldNotBe(Guid.Empty);
        cart.ClientId.ShouldNotBe(Guid.Empty);
        cart.CreatedAt.ShouldNotBe(default);
        cart.UpdatedAt.ShouldNotBe(default);
        cart.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        cart.UpdatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        cart.Lines.ShouldNotBeNull();
        return cart;
    }

    private static void EqualCart(ShoppingCartResponseDto actual, ShoppingCartResponseDto expected)
    {
        actual.Id.ShouldBe(expected.Id);
        actual.ClientId.ShouldBe(expected.ClientId);
        Milliseconds(actual.CreatedAt).ShouldBe(Milliseconds(expected.CreatedAt));
        Milliseconds(actual.UpdatedAt).ShouldBe(Milliseconds(expected.UpdatedAt));
        actual.Lines.ShouldBe(expected.Lines, ignoreOrder: true);
    }

    private static DateTime Milliseconds(DateTime value) => new(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);

    private static void InWindow(DateTime value, DateTime started, DateTime completed)
    {
        value.Kind.ShouldBe(DateTimeKind.Utc);
        value.ShouldBeGreaterThanOrEqualTo(started);
        value.ShouldBeLessThanOrEqualTo(completed);
    }
}
