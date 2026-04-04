using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceStoreInvoice.API.Endpoints
{
    public static class InvoicesEndpoints
    {
        public static IEndpointRouteBuilder MapInvoicesEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/invoices").WithTags("Invoices");

            MapInvoicesQueries(group);
            MapInvoicesCommands(group);

            return group;
        }

        private static void MapInvoicesCommands(IEndpointRouteBuilder group)
        {
            group.MapPost("/{orderId:guid}", async (Guid orderId, IInvoiceService invoiceService) =>
            {
                var invoice = await invoiceService.CreateInvoiceForOrder(orderId);

                return Results.Ok(invoice);
            })
           .WithSummary("Create invoice for order by Id.")
           .WithDescription("Creates a new invoice when the order exists and does not already have an invoice.")
           .WithName("CreateInvoiceForOrder")
           .Produces<InvoiceResponseDto>(StatusCodes.Status200OK)
           .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<NotFoundProblemDetails>(StatusCodes.Status404NotFound)
           .Produces<ConflictProblemDetails>(StatusCodes.Status409Conflict)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }

        private static void MapInvoicesQueries(IEndpointRouteBuilder group)
        {
            group.MapGet("/{invoiceId:guid}", async (Guid invoiceId, IInvoiceService invoiceService) =>
            {
                var invoice = await invoiceService.GetInvoiceById(invoiceId);

                return Results.Ok(invoice);
            })
            .WithSummary("Get invoice by Id.")
            .WithDescription("Returns the invoice when the Id exists; 404 otherwise.")
            .WithName("GetInvoiceById")
            .Produces<InvoiceResponseDto>(StatusCodes.Status200OK)
            .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<NotFoundProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }
    }
}
