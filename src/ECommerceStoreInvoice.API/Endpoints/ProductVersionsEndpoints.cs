namespace ECommerceStoreInvoice.API.Endpoints
{
    public static class ProductVersionsEndpoints
    {
        public static IEndpointRouteBuilder MapProductVersionsEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/product-versions").WithTags("ProductVersions");

            MapProductVersionsQueries(group);
            MapProductVersionsCommands(group);

            return group;
        }

        private static void MapProductVersionsCommands(IEndpointRouteBuilder group)
        {
        }

        private static void MapProductVersionsQueries(IEndpointRouteBuilder group)
        {

        }
    }
}
