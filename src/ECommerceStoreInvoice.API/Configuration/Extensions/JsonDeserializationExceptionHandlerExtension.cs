using Microsoft.AspNetCore.Mvc;

namespace ECommerceStoreInvoice.API.Configuration.Extensions;

public static class JsonDeserializationExceptionHandlerExtension
{
    public static async Task HandleJsonDeserializationException(
        this HttpContext context,
        Exception exception,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogWarning(
            exception,
            "Invalid request body at path {RequestPath}. TraceId: {TraceId}",
            context.Request.Path,
            context.TraceIdentifier);

        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid JSON payload.",
            Detail = "The request body must contain valid JSON.",
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier
            }
        }, options: null, contentType: "application/problem+json", cancellationToken);
    }
}
