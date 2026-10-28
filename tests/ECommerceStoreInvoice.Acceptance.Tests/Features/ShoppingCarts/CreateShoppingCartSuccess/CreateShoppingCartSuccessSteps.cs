using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ShoppingCarts.CreateShoppingCartSuccess;

[Binding]
public sealed class CreateShoppingCartSuccessSteps(ScenarioApiContext context, ShoppingCartSuccessState state)
{
    [Given("I prepare the create shopping cart request")]
    public void GivenPrepareRequest(Table table)
    {
        var value = ShoppingCartSuccessState.Required(ShoppingCartSuccessState.Values(table), "ClientId");
        state.ClientId = value == "auto" ? Guid.NewGuid() : Guid.Parse(value);
        AllureJson.AttachObject("Create shopping cart request", new { state.ClientId }, context.JsonOptions);
    }

    [When("I submit the create shopping cart request")]
    public Task WhenCreate() => state.SendAsync("Create");

    [Then("the shopping cart response should match")]
    public Task ThenResponse(Table table)
    {
        AllureJson.AttachObject("Expected result table", ShoppingCartSuccessState.Values(table), context.JsonOptions);
        return state.ExpectResponseAsync(table);
    }
}
