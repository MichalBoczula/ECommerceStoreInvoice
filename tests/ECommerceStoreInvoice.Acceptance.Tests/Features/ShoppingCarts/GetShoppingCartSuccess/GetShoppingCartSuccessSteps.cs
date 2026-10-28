using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ShoppingCarts.GetShoppingCartSuccess;

[Binding]
public sealed class GetShoppingCartSuccessSteps(ScenarioApiContext context, ShoppingCartSuccessState state)
{
    [Given("I have an existing shopping cart for retrieval")]
    public Task GivenExistingCart() => state.CreateEmptyFixtureAsync("Get shopping cart setup");

    [Given("the get shopping cart request data is")]
    public void GivenRequest(Table table)
    {
        var values = ShoppingCartSuccessState.Values(table);
        ShoppingCartSuccessState.Required(values, "Method").ShouldBe("GET");
        ShoppingCartSuccessState.Required(values, "Endpoint").ShouldBe("/shopping-carts/client");
        ShoppingCartSuccessState.Required(values, "ClientId").ShouldBe("<existing shopping cart client id>");
        AllureJson.AttachObject("Get shopping cart request object", new
        {
            Method = "GET",
            Endpoint = $"/shopping-carts/client/{state.ClientId}",
            state.ClientId
        }, context.JsonOptions);
    }

    [When("I request the shopping cart by client id")]
    public Task WhenGet() => state.SendAsync("Get");

    [Then("the shopping cart is returned successfully")]
    public Task ThenResponse(Table table) => state.ExpectResponseAsync(table);

    [Then("the shopping cart response payload is")]
    public void ThenPayload(Table table) => state.ExpectPayload(table);
}
