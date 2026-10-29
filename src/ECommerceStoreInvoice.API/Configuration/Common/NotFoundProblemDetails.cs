using Microsoft.AspNetCore.Mvc;

namespace ECommerceStoreInvoice.API.Configuration.Common
{
    /// <summary>Complete six-field resource-not-found problem, including request correlation.</summary>
    public sealed class NotFoundProblemDetails : ProblemDetails
    {
        /// <summary>Request correlation identifier.</summary>
        public string TraceId { get; init; } = string.Empty;
    }
}
