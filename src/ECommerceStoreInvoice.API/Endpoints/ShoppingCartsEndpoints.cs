using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Services.Abstract;
using Microsoft.AspNetCore.Mvc;

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
            group.MapGet("/{id:guid}", async (Guid id, IShoppingCartService shoppingCartService) =>
            {
                return Results.Ok();
            })
            .WithSummary("Get shopping cart by Id.")
            .WithDescription("Returns a shopping cart by Id; 404 otherwise.")
            .WithName("GetShoppingCartById")
            .Produces<ShoppingCartResponseDto>(StatusCodes.Status200OK)
            .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<NotFoundProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }

        private static void MapShoppingCartsCommands(IEndpointRouteBuilder group)
        {
        }
    }
}
