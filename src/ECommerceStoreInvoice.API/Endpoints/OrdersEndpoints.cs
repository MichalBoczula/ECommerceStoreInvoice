using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Services.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceStoreInvoice.API.Endpoints
{
    public static class OrdersEndpoints
    {
        public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/orders").WithTags("Orders");

            MapOrdersQueries(group);
            MapOrdersCommands(group);

            return group;
        }

        private static void MapOrdersCommands(IEndpointRouteBuilder group)
        {
            group.MapPost("/{clientId:guid}", async (Guid clientId, IOrderService orderService) =>
            {
                var order = await orderService.CreateOrder(clientId);

                return Results.Ok(order);
            })
           .WithSummary("Create order.")
           .WithDescription("Creates a new order based on the current shopping cart for the provided client.")
           .WithName("CreateOrder")
           .Produces<OrderResponseDto>(StatusCodes.Status200OK)
           .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<NotFoundProblemDetails>(StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPatch("/{orderId:guid}/status", async (
                Guid orderId,
                UpdateOrderStatusRequestDto request,
                IOrderService orderService) =>
            {
                var order = await orderService.UpdateOrderStatus(orderId, request);

                return Results.Ok(order);
            })
           .WithSummary("Update order status.")
           .WithDescription("Updates order status while enforcing allowed status transitions.")
           .WithName("UpdateOrderStatus")
           .Produces<OrderResponseDto>(StatusCodes.Status200OK)
           .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<NotFoundProblemDetails>(StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }

        private static void MapOrdersQueries(IEndpointRouteBuilder group)
        {
            group.MapGet("/{orderId:guid}", async (Guid orderId, IOrderService orderService) =>
            {
                var order = await orderService.GetOrderByOrderId(orderId);

                return Results.Ok(order);
            })
            .WithSummary("Get order by Id.")
            .WithDescription("Returns the order when the Id exists; 404 otherwise.")
            .WithName("GetOrderById")
            .Produces<OrderResponseDto>(StatusCodes.Status200OK)
            .Produces<NotFoundProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }
    }
}
