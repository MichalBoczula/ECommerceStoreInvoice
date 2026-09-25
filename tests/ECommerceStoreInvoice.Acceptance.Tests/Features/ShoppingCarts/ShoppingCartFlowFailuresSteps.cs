using System.Net;
using System.Text.Json;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ShoppingCarts;

[Binding]
public sealed class ShoppingCartFlowFailuresSteps(ScenarioApiContext context)
{
    private readonly Guid _clientId = Guid.NewGuid();

    [Given("a shopping cart has already been created")]
    public async Task GivenCartAlreadyExists()
    {
        using var response = await context.HttpClient.PostAsync($"/shopping-carts/{_clientId}", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [When("I read a shopping cart with an empty client id")]
    public async Task WhenReadEmptyId() =>
        context.Response = await context.HttpClient.GetAsync($"/shopping-carts/client/{Guid.Empty}");

    [When("I create the same shopping cart again")]
    public async Task WhenCreateDuplicate() =>
        context.Response = await context.HttpClient.PostAsync($"/shopping-carts/{_clientId}", null);

    [Then("the cart response is problem {int}")]
    public async Task ThenCartProblem(int status)
    {
        context.Response.ShouldNotBeNull();
        context.Response.StatusCode.ShouldBe((HttpStatusCode)status);
        context.Response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var json = JsonDocument.Parse(await context.Response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("status").GetInt32().ShouldBe(status);
        json.RootElement.GetProperty("instance").GetString().ShouldNotBeNullOrWhiteSpace();
    }
}
