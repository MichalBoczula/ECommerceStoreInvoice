using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Mapping;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Descriptors.ClientDataVersions
{
    internal sealed record GetClientDataVersionByClientId;

    internal sealed class GetClientDataVersionByClientIdDescriptor : FlowDescriberBase<GetClientDataVersionByClientId>
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

        [FlowStep(order: 3, bpmnId: "LoadClientDataVersion")]
        public async Task<ClientDataVersion?> Load(Guid clientId, IClientDataVersionRepository clientDataVersionRepository)
        {
            return await clientDataVersionRepository.GetByClientId(clientId);
        }

        [FlowStep(order: 4, bpmnId: "IsClientDataVersionExists")]
        public void ThrowNotFoundExceptionIfClientDataVersionMissing(Guid clientId, ClientDataVersion? clientDataVersion)
        {
            if (clientDataVersion is null)
            {
                throw new ResourceNotFoundException(nameof(Load), clientId, nameof(ClientDataVersion));
            }
        }

        [FlowStep(order: 5, bpmnId: "MapClientDataVersionResponse")]
        public ClientDataVersionResponseDto MapToResponse(ClientDataVersion clientDataVersion)
        {
            return MappingConfig.MapToResponse(clientDataVersion);
        }
    }
}
