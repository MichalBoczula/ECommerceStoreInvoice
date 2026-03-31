using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Common.ResponsesDto
{
    public sealed record ValidationDescriptorsResponseDto
    {
        public required List<Dictionary<string, ValidationPolicyDescriptor>> Validations { get; init; }
    }
}
