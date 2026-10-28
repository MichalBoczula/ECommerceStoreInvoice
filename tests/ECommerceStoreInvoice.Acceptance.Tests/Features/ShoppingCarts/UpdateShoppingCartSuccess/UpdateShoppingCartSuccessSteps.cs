using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ShoppingCarts.UpdateShoppingCartSuccess;

[Binding]
public sealed class UpdateShoppingCartSuccessSteps(ScenarioApiContext context, ShoppingCartSuccessState state)
{
    [Given("I have a valid update shopping cart request")]
    public void GivenRequest(Table table)
    {
        var values = ShoppingCartSuccessState.Values(table);
        state.RequestLines = ShoppingCartSuccessState.Lines(values, ShoppingCartSuccessState.Number(values, "LinesCount"));
        AllureJson.AttachObject("Update shopping cart request (from Gherkin table)", new { Lines = state.RequestLines }, context.JsonOptions);
    }

    [When("I submit the update shopping cart request")]
    public Task WhenUpdate() => state.SendAsync("Update");

    [Then("the shopping cart is updated successfully")]
    public Task ThenResponse(Table table) => state.ExpectResponseAsync(table);
}
