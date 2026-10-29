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

    [Documentation("inv.flow.get-client-data-version-by-client-id", "Read the latest immutable client version by creation timestamp within that client.")]

    internal sealed class GetClientDataVersionByClientIdDescriptor : FlowDescriberBase<GetClientDataVersionByClientId>
    {
        [FlowStep(order: 1, bpmnId: "ValidateClientId", description: "Validate the client identifier with the registered GUID policy.")]
        public async Task<ValidationResult> ValidateClientId(Guid clientId, IValidationPolicy<Guid> guidValidationPolicy)
        {
            return await guidValidationPolicy.Validate(clientId);
        }

        [FlowStep(order: 2, bpmnId: "IsClientIdValid", description: "Reject the flow with the complete registered validation errors when client id invalid.")]
        public void ThrowValidationExceptionIfClientIdInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 3, bpmnId: "LoadClientDataVersion", description: "Load the latest stored client version by creation time for the selected client.")]
        public async Task<ClientDataVersion?> Load(Guid clientId, IClientDataVersionRepository clientDataVersionRepository)
        {
            return await clientDataVersionRepository.GetByClientId(clientId);
        }

        [FlowStep(order: 4, bpmnId: "IsClientDataVersionExists", description: "Reject when no client data version exists for the requested client.")]
        public void ThrowNotFoundExceptionIfClientDataVersionMissing(Guid clientId, ClientDataVersion? clientDataVersion)
        {
            if (clientDataVersion is null)
            {
                throw new ResourceNotFoundException(nameof(Load), clientId, nameof(ClientDataVersion));
            }
        }

        [FlowStep(order: 5, bpmnId: "MapClientDataVersionResponse", description: "Return the complete get client data version by client id response from the domain values; this description is not execution tracing.")]
        public ClientDataVersionResponseDto MapToResponse(ClientDataVersion clientDataVersion)
        {
            return MappingConfig.MapToResponse(clientDataVersion);
        }
    }
}
