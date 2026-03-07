using Microsoft.AspNetCore.Mvc;

namespace ECommerceStoreInvoice.API.Configuration.Common
{
    public sealed class NotFoundProblemDetails : ProblemDetails
    {
        public string TraceId { get; init; } = string.Empty;
    }
}
