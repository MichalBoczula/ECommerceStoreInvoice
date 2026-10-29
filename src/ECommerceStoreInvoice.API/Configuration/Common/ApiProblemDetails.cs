using ECommerceStoreInvoice.Domain.Validation.Common;
using Microsoft.AspNetCore.Mvc;
namespace ECommerceStoreInvoice.API.Configuration.Common
{
    /// <summary>Complete business validation problem. All seven fields are emitted; malformed bodies use the distinct six-field ProblemDetails representation.</summary>
    public sealed class ApiProblemDetails : ProblemDetails
    {
        /// <summary>Complete array of business validation errors.</summary>
        public IEnumerable<ValidationError> Errors { get; init; } = Enumerable.Empty<ValidationError>();
        /// <summary>Request correlation identifier; it does not expose an exception stack.</summary>
        public string TraceId { get; init; } = string.Empty;
    }
}
