using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.API.Configuration.Extensions;

public static class OrderWriteConflictExceptionHandlerExtension
{
    public static async Task HandleOrderWriteConflictException(
        this HttpContext context,
        OrderWriteConflictException exception,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Concurrent order update for {OrderId} at {RequestPath}. TraceId: {TraceId}",
            exception.OrderId, context.Request.Path, context.TraceIdentifier);

        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new ConflictProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict.",
            Detail = "Order was changed by another request. Reload it before trying again.",
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
            Instance = context.Request.Path,
            TraceId = context.TraceIdentifier
        }, options: null, contentType: "application/problem+json", cancellationToken);
    }
}
