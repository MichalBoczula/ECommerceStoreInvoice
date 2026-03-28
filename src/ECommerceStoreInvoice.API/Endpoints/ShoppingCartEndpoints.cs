using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Services.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceStoreInvoice.API.Endpoints
{
    public static class ShoppingCartEndpoints
    {
        public static IEndpointRouteBuilder MapShoppingCartEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/shopping-carts").WithTags("Shopping Carts");

            MapShoppingCartQueries(group);
            MapShoppingCartCommands(group);

            return group;
        }

        private static void MapShoppingCartQueries(IEndpointRouteBuilder group)
        {
            group.MapGet("/client/{clientId:guid}", async (Guid clientId, IShoppingCartService shoppingCartService) =>
            {
                var shoppingCart = await shoppingCartService.GetShoppingCartByClientId(clientId);

                if (shoppingCart is null)
                {
                    throw new ResourceNotFoundException(
                        "ShoppingCart",
                        clientId,
                        "ClientId");
                }

                return Results.Ok(shoppingCart);
            })
            .WithSummary("Get shopping cart by client Id.")
            .WithDescription("Returns the shopping cart assigned to the provided client identifier when it exists; 404 otherwise.")
            .WithName("GetShoppingCartByClientId")
            .Produces<ShoppingCartResponseDto>(StatusCodes.Status200OK)
            .Produces<NotFoundProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }

        private static void MapShoppingCartCommands(IEndpointRouteBuilder group)
        {
            group.MapPost("/{clientId:guid}", async (Guid clientId, IShoppingCartService shoppingCartService) =>
            {
                var shoppingCart = await shoppingCartService.CreateShoppingCart(clientId);

                return Results.Ok(shoppingCart);
            })
            .WithSummary("Create shopping cart.")
            .WithDescription("Creates a new shopping cart for the provided client identifier.")
            .WithName("CreateShoppingCart")
            .Produces<ShoppingCartResponseDto>(StatusCodes.Status200OK)
            .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/{clientId:guid}", async (
                Guid clientId,
                UpdateShoppingCartRequestDto request,
                IShoppingCartService shoppingCartService) =>
            {
                var shoppingCart = await shoppingCartService.UpdateShoppingCart(clientId, request);

                return Results.Ok(shoppingCart);
            })
            .WithSummary("Update shopping cart.")
            .WithDescription("Updates shopping cart lines for the provided client identifier.")
            .WithName("UpdateShoppingCart")
            .Produces<ShoppingCartResponseDto>(StatusCodes.Status200OK)
            .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<NotFoundProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }
    }
}