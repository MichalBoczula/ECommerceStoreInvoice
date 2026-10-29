using Microsoft.AspNetCore.Mvc;

namespace ECommerceStoreInvoice.API.Configuration.Common
{
    /// <summary>Complete six-field duplicate-resource or stale-write conflict problem.</summary>
    public sealed class ConflictProblemDetails : ProblemDetails
    {
        /// <summary>Request correlation identifier.</summary>
        public string TraceId { get; init; } = string.Empty;
    }
}
