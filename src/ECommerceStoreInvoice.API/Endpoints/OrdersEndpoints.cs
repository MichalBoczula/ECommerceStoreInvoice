using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Services.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
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
            group.MapPost("/checkout/{shoppingCartId:guid}", async (Guid shoppingCartId, IInvoiceService invoiceService) =>
            {
                throw new ResourceNotFoundException("test", Guid.NewGuid(), "test");
            })
           .WithSummary("Checkout create order.")
           .WithDescription("Returns newly created order based on shopping cart identify by id.")
           .WithName("CreateOrder")
           .Produces<InvoiceResponseDto>(StatusCodes.Status200OK)
           .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }

        private static void MapOrdersQueries(IEndpointRouteBuilder group)
        {
            group.MapGet("/{orderId:guid}", async (Guid orderId, IInvoiceService invoiceService) =>
            {
                throw new ResourceNotFoundException("test", Guid.NewGuid(), "test");
            })
            .WithSummary("Get order by Id.")
            .WithDescription("Returns the invoice order when the Id exists; 404 otherwise.")
            .WithName("GetOrderById")
            .Produces<InvoiceResponseDto>(StatusCodes.Status200OK)
            .Produces<NotFoundProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }
    }
}
