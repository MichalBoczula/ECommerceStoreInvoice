using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Services.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
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
        }

        private static void MapInvoicesQueries(IEndpointRouteBuilder group)
        {
            group.MapGet("/{id:guid}", async (Guid id, IInvoiceService invoiceService) =>
            {
                throw new ResourceNotFoundException("test", Guid.NewGuid(), "test");
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
