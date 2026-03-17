using ECommerceStoreInvoice.API.Configuration.Common;
using Microsoft.AspNetCore.Mvc;

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
            group.MapGet("/{id:guid}", (Guid id) =>
            {
                return Results.Ok(new ProductVersionResponse(id, "Blueprint product version"));
            })
            .WithSummary("Get product version by Id.")
            .WithDescription("Returns a product version blueprint response.")
            .WithName("GetProductVersionById")
            .Produces<ProductVersionResponse>(StatusCodes.Status200OK)
            .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }

        private sealed record ProductVersionResponse(Guid Id, string Name);
    }
}
