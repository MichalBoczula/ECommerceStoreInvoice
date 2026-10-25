using ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Application.Services.Concrete.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace ECommerceStoreInvoice.Application.UnitTests.Services.Invoices;

public sealed class InvoiceDownloadTests
{
    [Theory]
    [InlineData("invalid")]
    [InlineData("missing-invoice")]
    [InlineData("missing-pdf")]
    [InlineData("completed")]
    public async Task DownloadValidatesAndReadsOnlyCompletedInvoice(string state)
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IInvoiceRepository>();
        var pdf = new Mock<IInvoicePdfService>();
        var validation = new Mock<IValidationPolicy<Guid>>();
        var result = new ValidationResult();
        if (state == "invalid") result.AddValidationError(new ValidationError { Entity = "Invoice", Name = "Id", Message = "Invalid" });
        validation.Setup(x => x.Validate(id)).ReturnsAsync(result);
        var invoice = Invoice.Rehydrate(id, Guid.NewGuid(), Guid.NewGuid(), "https://account/invoices/demo.pdf", DateTime.UtcNow);
        repository.Setup(x => x.GetInvoiceById(id)).ReturnsAsync(state == "missing-invoice" ? null : invoice);
        var content = new MemoryStream("%PDF-example"u8.ToArray());
        pdf.Setup(x => x.OpenReadAsync(invoice.StorageUrl, default)).ReturnsAsync(state == "missing-pdf" ? null : content);
        var sut = new InvoiceService(repository.Object, Mock.Of<IInvoiceGenerationRepository>(), Mock.Of<IOrderRepository>(),
            Mock.Of<IClientDataVersionService>(), pdf.Object, validation.Object,
            Mock.Of<IValidationPolicy<InvoiceOrderStatusValidationContext>>(), NullLogger<InvoiceService>.Instance);
        if (state == "invalid")
        {
            await Should.ThrowAsync<ValidationException>(() => sut.DownloadInvoicePdf(id));
            repository.Verify(x => x.GetInvoiceById(It.IsAny<Guid>()), Times.Never);
        }
        else if (state.StartsWith("missing", StringComparison.Ordinal))
            await Should.ThrowAsync<ResourceNotFoundException>(() => sut.DownloadInvoicePdf(id));
        else (await sut.DownloadInvoicePdf(id)).ShouldBeSameAs(content);
    }
}
