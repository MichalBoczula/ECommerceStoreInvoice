using Microsoft.Extensions.Logging;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Descriptors.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;

namespace ECommerceStoreInvoice.Application.Services.Concrete.ClientDataVersions
{
    internal sealed class ClientDataVersionService(
        IClientDataVersionRepository clientDataVersionRepository,
        IValidationPolicy<Guid> guidValidationPolicy,
        IValidationPolicy<ClientDataVersion> clientDataVersionValidationPolicy,
        ILogger<ClientDataVersionService> logger)
        : IClientDataVersionService
    {
        public async Task<ClientDataVersionResponseDto> Create(Guid clientId, CreateClientDataVersionRequestDto request)
        {
            logger.LogInformation("Initiating client data version creation for ClientId: {ClientId}", clientId);

            var descriptor = new CreateClientDataVersionDescriptor();

            var validationResult = await descriptor.ValidateClientId(clientId, guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfClientIdInvalid(validationResult);

            var clientDataVersion = descriptor.MapToDomain(clientId, request);

            validationResult = await descriptor.ValidateClientDataVersion(clientDataVersion, clientDataVersionValidationPolicy);
            descriptor.ThrowValidationExceptionIfClientDataVersionInvalid(validationResult);

            var createdClientDataVersion = await descriptor.Save(clientDataVersion, clientDataVersionRepository);

            logger.LogInformation("Successfully created client data version. ClientDataVersionId: {ClientDataVersionId} for ClientId: {ClientId}", createdClientDataVersion.Id, clientId);

            return descriptor.MapToResponse(createdClientDataVersion);
        }

        public async Task<ClientDataVersionResponseDto> GetByClientId(Guid clientId)
        {
            logger.LogInformation("Initiating read flow for latest client data version by ClientId: {ClientId}", clientId);

            var descriptor = new GetClientDataVersionByClientIdDescriptor();

            var validationResult = await descriptor.ValidateClientId(clientId, guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfClientIdInvalid(validationResult);

            var clientDataVersion = await descriptor.Load(clientId, clientDataVersionRepository);
            descriptor.ThrowNotFoundExceptionIfClientDataVersionMissing(clientId, clientDataVersion);

            logger.LogInformation("Successfully fetched latest client data version for ClientId: {ClientId}. ClientDataVersionId: {ClientDataVersionId}", clientId, clientDataVersion!.Id);

            return descriptor.MapToResponse(clientDataVersion);
        }
    }
}
