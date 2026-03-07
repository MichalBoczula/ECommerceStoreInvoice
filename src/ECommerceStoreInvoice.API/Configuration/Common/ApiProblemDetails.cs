using ECommerceStoreInvoice.Domain.Validation.Common;
using Microsoft.AspNetCore.Mvc;
namespace ECommerceStoreInvoice.API.Configuration.Common
{
    public sealed class ApiProblemDetails : ProblemDetails
    {
        public IEnumerable<ValidationError> Errors { get; init; } = Enumerable.Empty<ValidationError>();
        public string TraceId { get; init; } = string.Empty;
    }
}
