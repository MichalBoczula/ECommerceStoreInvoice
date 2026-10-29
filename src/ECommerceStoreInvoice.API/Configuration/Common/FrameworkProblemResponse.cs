using Microsoft.AspNetCore.Mvc;

namespace ECommerceStoreInvoice.API.Configuration.Common;

internal static class FrameworkProblemResponse
{
    internal static Task WriteAsync(HttpContext context, int status, CancellationToken cancellationToken)
    {
        var (title, detail, section) = status switch
        {
            404 => ("Route not found.", "The requested route does not exist.", "6.5.4"),
            405 => ("Method not allowed.", "The requested method is not supported by this route.", "6.5.5"),
            415 => ("Unsupported media type.", "The request content type is not supported.", "6.5.13"),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
        context.Response.StatusCode = status;
        context.Response.Headers.CacheControl = "no-cache, no-store";
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-" + section,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, options: null, contentType: "application/problem+json", cancellationToken);
    }
}
