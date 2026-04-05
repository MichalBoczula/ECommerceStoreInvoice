using Microsoft.AspNetCore.Mvc;

namespace ECommerceStoreInvoice.API.Configuration.Extensions
{
    internal static class DefaultExceptionHandlerExtension
    {
        public static async Task HandleDefaultException(
            this HttpContext context,
            Exception exception,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var title = exception.GetType().Name;
            var detail = string.IsNullOrWhiteSpace(exception.Message)
                ? "An unexpected error occurred."
                : exception.Message;

            logger.LogError(
                exception,
                "Unhandled exception: {ExceptionTitle}. Message: {ExceptionMessage}",
                title,
                detail);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = title,
                Detail = detail,
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                Instance = context.Request.Path,
                Extensions =
                {
                    ["traceId"] = context.TraceIdentifier
                }
            }, cancellationToken);
        }
    }
}