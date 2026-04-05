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
        IValidationPolicy<ClientDataVersion> clientDataVersionValidationPolicy)
        : IClientDataVersionService
    {
        public async Task<ClientDataVersionResponseDto> Create(Guid clientId, CreateClientDataVersionRequestDto request)
        {
            var descriptor = new CreateClientDataVersionDescriptor();

            var validationResult = await descriptor.ValidateClientId(clientId, guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfClientIdInvalid(validationResult);

            var clientDataVersion = descriptor.MapToDomain(clientId, request);

            validationResult = await descriptor.ValidateClientDataVersion(clientDataVersion, clientDataVersionValidationPolicy);
            descriptor.ThrowValidationExceptionIfClientDataVersionInvalid(validationResult);

            var createdClientDataVersion = await descriptor.Save(clientDataVersion, clientDataVersionRepository);

            return descriptor.MapToResponse(createdClientDataVersion);
        }

        public async Task<ClientDataVersionResponseDto> GetByClientId(Guid clientId)
        {
            var descriptor = new GetClientDataVersionByClientIdDescriptor();

            var validationResult = await descriptor.ValidateClientId(clientId, guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfClientIdInvalid(validationResult);

            var clientDataVersion = await descriptor.Load(clientId, clientDataVersionRepository);
            descriptor.ThrowNotFoundExceptionIfClientDataVersionMissing(clientId, clientDataVersion);

            return descriptor.MapToResponse(clientDataVersion!);
        }
    }
}
