using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.API.Configuration.Extensions;

public static class NotFoundExceptionHandlerExtension
{
    public static async Task HandleNotFoundException(
        this HttpContext context,
        ResourceNotFoundException exception,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Resource tracking failure: Type {ResourceType} with Id {ResourceId} was not found during action {ActionName} at path {RequestPath}. TraceId: {TraceId}",
            exception.ResourceType,
            exception.ResourceId,
            exception.ActionName,
            context.Request.Path,
            context.TraceIdentifier);

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new NotFoundProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Resource not found.",
            Detail = $"Resource {exception.ResourceType} identified by id {exception.ResourceId} cannot be found in database during action {exception.ActionName}.",
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
            Instance = context.Request.Path,
            TraceId = context.TraceIdentifier
        }, cancellationToken);
    }
}