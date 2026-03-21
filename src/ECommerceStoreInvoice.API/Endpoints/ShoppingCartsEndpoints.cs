namespace ECommerceStoreInvoice.API.Endpoints
{
    public static class ShoppingCartsEndpoints
    {
        public static IEndpointRouteBuilder MapShoppingCartsEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/shopping-carts").WithTags("ShoppingCarts");

            MapShoppingCartsQueries(group);
            MapShoppingCartsCommands(group);

            return group;
        }

        private static void MapShoppingCartsQueries(IEndpointRouteBuilder group)
        {
        }

        private static void MapShoppingCartsCommands(IEndpointRouteBuilder group)
        {
        }
    }
}
