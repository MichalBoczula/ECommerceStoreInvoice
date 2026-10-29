using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Common.ResponsesDto
{
    /// <summary>Complete registered validation-policy documentation; reading it does not execute those policies.</summary>
    public sealed record ValidationDescriptorsResponseDto
    {
        /// <summary>Ordered policy dictionaries keyed by policy name.</summary>
        public required List<Dictionary<string, ValidationPolicyDescriptor>> Validations { get; init; }
    }
}
