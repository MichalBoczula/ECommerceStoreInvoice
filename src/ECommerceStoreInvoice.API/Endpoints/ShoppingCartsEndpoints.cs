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
            group.MapPut("/add-item", async (Guid shoppingCartId, IShoppingCartService shoppingCartService) =>
            {
                return Results.Ok();
            })
           .WithSummary("Add item to shopping cart, or change quantity of existing.")
           .WithDescription("Returns a updated shopping cart.")
           .WithName("AddItemToShoppingCart")
           .Produces<ShoppingCartResponseDto>(StatusCodes.Status200OK)
           .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<NotFoundProblemDetails>(StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/remove-item", async (Guid shoppingCartId, IShoppingCartService shoppingCartService) =>
            {
                return Results.Ok();
            })
           .WithSummary("Add item to shopping cart, or change quantity of existing.")
           .WithDescription("Returns a updated shopping cart.")
           .WithName("RemoveItemFromShoppingCart")
           .Produces<ShoppingCartResponseDto>(StatusCodes.Status200OK)
           .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<NotFoundProblemDetails>(StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/checkout", async (Guid shoppingCartId, IShoppingCartService shoppingCartService) =>
            {
                return Results.Ok();
            })
           .WithSummary("Create order based on actual shopping cart.")
           .WithDescription("Create and returns a order.")
           .WithName("Checkout")
           .Produces<ShoppingCartResponseDto>(StatusCodes.Status200OK)
           .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<NotFoundProblemDetails>(StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapDelete("/{shoppingCartId:guid}", async (Guid shoppingCartId, IShoppingCartService shoppingCartService) =>
            {
                return Results.Ok();
            })
           .WithSummary("Remove shoping cart identify by Id.")
           .WithDescription("Delete shopping cart.")
           .WithName("DeleteShoppingCart")
           .Produces<ShoppingCartResponseDto>(StatusCodes.Status200OK)
           .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<NotFoundProblemDetails>(StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }
    }
}
