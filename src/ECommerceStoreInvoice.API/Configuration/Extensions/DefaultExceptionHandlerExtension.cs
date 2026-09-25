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
            logger.LogError(
                exception,
                "Unhandled exception at path {RequestPath}. TraceId: {TraceId}",
                context.Request.Path,
                context.TraceIdentifier);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server error.",
                Detail = "An unexpected error occurred.",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                Instance = context.Request.Path,
                Extensions =
                {
                    ["traceId"] = context.TraceIdentifier
                }
            }, options: null, contentType: "application/problem+json", cancellationToken);
        }
    }
}
