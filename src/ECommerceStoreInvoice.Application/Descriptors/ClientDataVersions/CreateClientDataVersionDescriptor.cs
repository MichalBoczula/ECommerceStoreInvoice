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

    [Documentation("inv.flow.create-client-data-version", "Validate and append a new immutable client version without rewriting older versions or invoice references.")]

    internal sealed class CreateClientDataVersionDescriptor : FlowDescriberBase<CreateClientDataVersion>
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

        [FlowStep(order: 3, bpmnId: "MapRequestToDomain", description: "Construct the create client data version domain values without saving them.")]
        public ClientDataVersion MapToDomain(Guid clientId, CreateClientDataVersionRequestDto request)
        {
            return MappingConfig.MapToDomain(clientId, request);
        }

        [FlowStep(order: 4, bpmnId: "ValidateClientDataVersion", description: "Apply the registered phone, email and address policies without rewriting accepted null or whitespace values.")]
        public async Task<ValidationResult> ValidateClientDataVersion(ClientDataVersion clientDataVersion, IValidationPolicy<ClientDataVersion> clientDataVersionValidationPolicy)
        {
            return await clientDataVersionValidationPolicy.Validate(clientDataVersion);
        }

        [FlowStep(order: 5, bpmnId: "IsClientDataVersionValid", description: "Reject the flow with the complete registered validation errors when client data version invalid.")]
        public void ThrowValidationExceptionIfClientDataVersionInvalid(ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        [FlowStep(order: 6, bpmnId: "SaveClientDataVersion", description: "Persist the newly validated create client data version values without rewriting historical versions.")]
        public async Task<ClientDataVersion> Save(ClientDataVersion clientDataVersion, IClientDataVersionRepository clientDataVersionRepository)
        {
            await clientDataVersionRepository.Create(clientDataVersion);
            return clientDataVersion;
        }

        [FlowStep(order: 7, bpmnId: "MapClientDataVersionResponse", description: "Return the complete create client data version response from the domain values; this description is not execution tracing.")]
        public ClientDataVersionResponseDto MapToResponse(ClientDataVersion clientDataVersion)
        {
            return MappingConfig.MapToResponse(clientDataVersion);
        }
    }
}
