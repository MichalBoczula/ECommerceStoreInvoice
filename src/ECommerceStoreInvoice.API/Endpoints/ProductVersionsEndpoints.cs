using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Services.Abstract;
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
            group.MapGet("/{id:guid}", async (Guid id, IProductVersionService productVersionService) =>
            {
                return Results.Ok();
            })
            .WithSummary("Get product version by Id.")
            .WithDescription("Returns a product snapshot by id; 404 otherwise.")
            .WithName("GetProductVersionById")
            .Produces<ProductVersionResponseDto>(StatusCodes.Status200OK)
            .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<NotFoundProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }
    }
}
