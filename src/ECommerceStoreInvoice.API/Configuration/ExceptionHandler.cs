using System.Text.Json;
using ECommerceStoreInvoice.API.Configuration.Extensions;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace ECommerceStoreInvoice.API.Configuration
{
    public sealed class ExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<ExceptionHandler> _logger;

        public ExceptionHandler(ILogger<ExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext context,
            Exception exception,
            CancellationToken cancellationToken)
        {
            await (exception switch
            {
                ValidationException validationException =>
                    ValidationExceptionHandlerExtension.HandleValidationException(
                        context, validationException, cancellationToken),

                ResourceNotFoundException notFoundException =>
                    NotFoundExceptionHandlerExtension.HandleNotFoundException(
                        context, notFoundException, cancellationToken),

                BadHttpRequestException badHttpRequestException when badHttpRequestException.InnerException is JsonException =>
                    JsonDeserializationExceptionHandlerExtension.HandleJsonDeserializationException(
                        context, badHttpRequestException, cancellationToken),

                JsonException jsonException =>
                    JsonDeserializationExceptionHandlerExtension.HandleJsonDeserializationException(
                        context, jsonException, cancellationToken),

                ResourceAlreadyExistsException resourceAlreadyExistsException =>
                    ResourceAlreadyExistsExceptionHandlerExtension.HandleResourceAlreadyExistsException(
                        context, resourceAlreadyExistsException, cancellationToken),

                _ => DefaultExceptionHandlerExtension.HandleDefaultException(context, exception, _logger, cancellationToken)
            });

            return true;
        }
    }
}
