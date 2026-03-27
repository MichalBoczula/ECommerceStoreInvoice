using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using MongoDB.Driver;

namespace ECommerceStoreInvoice.Infrastructure.Repositories
{
    internal sealed class ShoppingCartRepository : IShoppingCartRepository
    {
        private readonly MongoDbContext _context;

        public ShoppingCartRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<ShoppingCart?> GetShoppingCartById(Guid shoppingCartId)
        {
            var shoppingCartDocument = await _context.ShoppingCarts
                .Find(x => x.Id == shoppingCartId)
                .FirstOrDefaultAsync();

            if (shoppingCartDocument is null)
                throw new InvalidOperationException($"Shopping cart with id '{shoppingCartId}' was not found.");

            return ShoppingCartMapping.MapToDomain(shoppingCartDocument);
        }

        public async Task<ShoppingCart> CreateShoppingCart(ShoppingCart shoppingCart)
        {
            var shoppingCartDocument = ShoppingCartMapping.MapToDocument(shoppingCart);

            await _context.ShoppingCarts.InsertOneAsync(shoppingCartDocument);

            return shoppingCart;
        }

        public async Task<ShoppingCart> UpdateShoppingCart(ShoppingCart shoppingCart)
        {
            var shoppingCartDocument = ShoppingCartMapping.MapToDocument(shoppingCart);

            var result = await _context.ShoppingCarts.ReplaceOneAsync(
                x => x.Id == shoppingCartDocument.Id,
                shoppingCartDocument);

            if (result.MatchedCount == 0)
                throw new InvalidOperationException($"Shopping cart with id '{shoppingCart.Id}' was not found.");

            return shoppingCart;
        }

        public async Task DeleteShoppingCart(Guid shoppingCartId)
        {
            var result = await _context.ShoppingCarts.DeleteOneAsync(x => x.Id == shoppingCartId);

            if (result.DeletedCount == 0)
                throw new InvalidOperationException($"Shopping cart with id '{shoppingCartId}' was not found.");
        }
    }
}