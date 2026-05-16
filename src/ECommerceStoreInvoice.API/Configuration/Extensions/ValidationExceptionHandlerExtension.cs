using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Domain.Validation.Common;
namespace ECommerceStoreInvoice.API.Configuration.Extensions
{
    public static class ValidationExceptionHandlerExtension
    {
        public static async Task HandleValidationException(
            this HttpContext context,
            ValidationException validationException,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            logger.LogWarning(
                "Validation failure: Request at path {RequestPath} failed validation with {ErrorCount} error(s). TraceId: {TraceId}",
                context.Request.Path,
                validationException.ValidationResult.GetValidationErrors().Count,
                context.TraceIdentifier);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new ApiProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed.",
                Detail = "One or more validation errors occurred.",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                Instance = context.Request.Path,
                TraceId = context.TraceIdentifier,
                Errors = validationException.ValidationResult.GetValidationErrors()
            }, cancellationToken);
        }
    }
}
