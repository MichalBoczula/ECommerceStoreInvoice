using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.ClientDataVersions
{
    internal sealed record CreateClientDataVersion;

    internal sealed class CreateClientDataVersionDescriptor : FlowDescriberBase<CreateClientDataVersion>
    {
        [FlowStep(order: 1, bpmnId: "ValidateClientId")]
        public async Task<ValidationResult> ValidateClientId(Guid clientId, IValidationPolicy<Guid> guidValidationPolicy)
        {
            return await guidValidationPolicy.Validate(clientId);
        }

        [FlowStep(order: 2, bpmnId: "IsClientIdValid")]
        public void ThrowValidationExceptionIfClientIdInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 3, bpmnId: "MapRequestToDomain")]
        public ClientDataVersion MapToDomain(Guid clientId, CreateClientDataVersionRequestDto request)
        {
            return MappingConfig.MapToDomain(clientId, request);
        }

        [FlowStep(order: 4, bpmnId: "SaveClientDataVersion")]
        public async Task<ClientDataVersion> Save(ClientDataVersion clientDataVersion, IClientDataVersionRepository clientDataVersionRepository)
        {
            await clientDataVersionRepository.Create(clientDataVersion);
            return clientDataVersion;
        }

        [FlowStep(order: 5, bpmnId: "MapClientDataVersionResponse")]
        public ClientDataVersionResponseDto MapToResponse(ClientDataVersion clientDataVersion)
        {
            return MappingConfig.MapToResponse(clientDataVersion);
        }
    }
}
