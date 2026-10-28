using Reqnroll;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.ShoppingCarts;

[Binding]
public sealed class ShoppingCartPersistenceSteps(ShoppingCartSuccessState state)
{
    [Given("a stored shopping cart for the target client")]
    public Task GivenTargetCart(Table table) => state.SeedAsync(table, other: false);

    [Given("a stored shopping cart for another client")]
    public Task GivenOtherCart(Table table) => state.SeedAsync(table, other: true);

    [Then("the shopping cart database and supporting reads match")]
    public Task ThenDatabase(Table table) => state.ExpectDatabaseAsync(table);
}
