namespace ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories
{
    public interface IShoppingCartRepository
    {
        public Task<ShoppingCart?> GetShoppingCartById(Guid shoppingCartId);
        public Task<ShoppingCart> CreateShoppingCart(ShoppingCart shoppingCart);
        public Task<ShoppingCart> UpdateShoppingCart(ShoppingCart shoppingCart);
        public Task DeleteShoppingCart(Guid shoppingCartId);
    }
}
