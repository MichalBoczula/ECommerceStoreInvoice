using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceStoreInvoice.API.Endpoints
{
    public static class ClientDataVersionsEndpoints
    {
        public static IEndpointRouteBuilder MapClientDataVersionsEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/client-data-versions").WithTags("ClientDataVersions");

            MapClientDataVersionQueries(group);
            MapClientDataVersionCommands(group);

            return group;
        }

        private static void MapClientDataVersionQueries(IEndpointRouteBuilder group)
        {
            group.MapGet("/client/{clientId:guid}", async (Guid clientId, IClientDataVersionService clientDataVersionService) =>
            {
                var clientDataVersion = await clientDataVersionService.GetByClientId(clientId);

                if (clientDataVersion is null)
                {
                    throw new ResourceNotFoundException(
                        "ClientDataVersion",
                        clientId,
                        "ClientId");
                }

                return Results.Ok(clientDataVersion);
            })
            .WithSummary("Get latest client data version by client Id.")
            .WithDescription("Returns the most recent client data version for the provided client id; 404 otherwise.")
            .WithName("GetClientDataVersionByClientId")
            .Produces<ClientDataVersionResponseDto>(StatusCodes.Status200OK)
            .Produces<NotFoundProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }

        private static void MapClientDataVersionCommands(IEndpointRouteBuilder group)
        {
            group.MapPost("/{clientId:guid}", async (
                Guid clientId,
                CreateClientDataVersionRequestDto request,
                IClientDataVersionService clientDataVersionService) =>
            {
                var clientDataVersion = await clientDataVersionService.Create(clientId, request);

                return Results.Ok(clientDataVersion);
            })
            .WithSummary("Create client data version.")
            .WithDescription("Creates a new client data version for the provided client identifier.")
            .WithName("CreateClientDataVersion")
            .Produces<ClientDataVersionResponseDto>(StatusCodes.Status200OK)
            .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }
    }
}
