using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ProductVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Services.Abstract.ProductVersions;
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
            group.MapPost("", async (
                CreateProductVersionRequestDto request,
                IProductVersionService productVersionService) =>
            {
                var productVersion = await productVersionService.CreateProductVersion(request);

                return Results.Ok(productVersion);
            })
            .WithSummary("Create product version.")
            .WithDescription("Creates a new product version.")
            .WithName("CreateProductVersion")
            .Produces<ProductVersionResponseDto>(StatusCodes.Status200OK)
            .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }

        private static void MapProductVersionsQueries(IEndpointRouteBuilder group)
        {
            group.MapGet("/{id:guid}", async (Guid id, IProductVersionService productVersionService) =>
            {
                var productVersion = await productVersionService.GetProductVersionById(id);
                return Results.Ok(productVersion);
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
