using Microsoft.Extensions.Logging;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions;
using ECommerceStoreInvoice.Application.Descriptors.Invoices;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Application.Services.Concrete.Invoices
{
    internal sealed class InvoiceService(
        IInvoiceRepository invoiceRepository,
        IInvoiceGenerationRepository generationRepository,
        IOrderRepository orderRepository,
        IClientDataVersionService clientDataVersionService,
        IInvoicePdfService invoicePdfService,
        IValidationPolicy<Guid> guidValidationPolicy,
        IValidationPolicy<InvoiceOrderStatusValidationContext> createInvoiceValidationPolicy,
        ILogger<InvoiceService> logger)
        : IInvoiceService
    {
        public async Task<InvoiceResponseDto> CreateInvoiceForOrder(Guid clientId, Guid orderId)
        {
            logger.LogInformation("Initiating invoice generation flow for OrderId: {OrderId} and ClientId: {ClientId}", orderId, clientId);

            var descriptor = new CreateInvoiceForOrderDescriptor();

            var validationResult = await descriptor.ValidateClientId(clientId, guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfIdInvalid(validationResult);

            validationResult = await descriptor.ValidateOrderId(orderId, guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfIdInvalid(validationResult);

            var orderWithProductVersions = await descriptor.LoadOrderWithProductVersions(orderId, orderRepository);

            descriptor.ThrowNotFoundExceptionIfOrderMissing(orderId, orderWithProductVersions);

            var (order, productVersions) = orderWithProductVersions!.Value;
            descriptor.ThrowNotFoundExceptionIfOrderOwnedByAnotherClient(clientId, orderId, order);

            var existingInvoice = await descriptor.LoadInvoiceByOrderId(orderId, invoiceRepository);
            descriptor.ThrowAlreadyExistsExceptionIfInvoiceAlreadyExists(orderId, existingInvoice);

            validationResult = await descriptor.ValidateOrderStatus(order, createInvoiceValidationPolicy);
            descriptor.ThrowValidationExceptionIfOrderStatusInvalid(validationResult);

            var clientDataVersion = await descriptor.LoadClientDataVersion(clientId, clientDataVersionService);
            var reservation = descriptor.CreateInvoiceReservation(orderId, clientDataVersion.Id);
            var attemptId = descriptor.CreateGenerationAttemptId();
            var claim = await descriptor.TryClaimInvoiceGeneration(reservation, attemptId, generationRepository);
            descriptor.ThrowAlreadyExistsExceptionIfClaimMissing(orderId, claim);

            // Completion can commit in Mongo even if the acknowledgement is lost. Keep the PDF
            // once completion has been attempted so a committed invoice never points to a deleted file.
            var completionAttempted = false;
            Invoice createdInvoice;
            try
            {
                var storageUrl = await descriptor.GenerateInvoicePdf(claim!, order, productVersions, clientDataVersion, invoicePdfService);
                completionAttempted = true;
                createdInvoice = await descriptor.CompleteInvoiceGeneration(claim!, storageUrl, generationRepository);
            }
            catch
            {
                try
                {
                    await descriptor.ReleaseFailedGeneration(claim!, generationRepository);
                }
                catch (Exception cleanupException)
                {
                    logger.LogError(cleanupException, "Could not release invoice generation for OrderId: {OrderId}", orderId);
                }

                if (!completionAttempted)
                {
                    try
                    {
                        await descriptor.DeleteFailedGenerationPdf(claim!, invoicePdfService);
                    }
                    catch (Exception cleanupException)
                    {
                        logger.LogError(cleanupException, "Could not delete incomplete PDF for OrderId: {OrderId}", orderId);
                    }
                }

                throw;
            }

            logger.LogInformation("Successfully completed invoice generation. InvoiceId: {InvoiceId} mapped to OrderId: {OrderId}", createdInvoice.Id, orderId);

            return descriptor.MapToResponse(createdInvoice);
        }

        public async Task<InvoiceResponseDto> GetInvoiceById(Guid invoiceId)
        {
            logger.LogDebug("Processing read request for InvoiceId: {InvoiceId}", invoiceId);

            var descriptor = new GetInvoiceByIdDescriptor();

            var validationResult = await descriptor.ValidateInvoiceId(invoiceId, guidValidationPolicy);
            descriptor.ThrowValidationExceptionIfInvoiceIdInvalid(validationResult);

            var invoice = await descriptor.LoadInvoiceById(invoiceId, invoiceRepository);
            descriptor.ThrowNotFoundExceptionIfInvoiceMissing(invoiceId, invoice);

            return descriptor.MapToResponse(invoice!);
        }
    }
}
