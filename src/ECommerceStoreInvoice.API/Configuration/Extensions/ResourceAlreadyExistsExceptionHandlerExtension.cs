using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.API.Configuration.Extensions
{
    public static class ResourceAlreadyExistsExceptionHandlerExtension
    {
        public static async Task HandleResourceAlreadyExistsException(this HttpContext context, ResourceAlreadyExistsException exception, CancellationToken cancellationToken)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new ConflictProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict.",
                Detail = $"Resource {exception.ResourceType} identified by id {exception.ResourceId} already exists in db. Error in action {exception.ActionName}.",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
                Instance = context.Request.Path,
                TraceId = context.TraceIdentifier
            }, cancellationToken);
        }
    }
}
