namespace ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories
{
    public interface IShoppingCartRepository
    {
        public Task<ShoppingCart?> GetShoppingCartByClientId(Guid clientId);
        public Task<ShoppingCart> CreateShoppingCart(ShoppingCart shoppingCart);
        public Task<ShoppingCart> UpdateShoppingCart(ShoppingCart shoppingCart);
    }
}
