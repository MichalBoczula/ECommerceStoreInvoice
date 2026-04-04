using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Domain.InvoiceAggregate
{
    public class InvoiceTests
    {
        [Fact]
        public void Ctor_ShouldInitializeInvoiceWithOrderIdStorageUrlAndCreatedAt()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            const string storageUrl = "https://storage.example.com/invoices/invoice-123.pdf";

            // Act
            var beforeCreation = DateTime.UtcNow;
            var invoice = new Invoice(orderId, storageUrl);
            var afterCreation = DateTime.UtcNow;

            // Assert
            invoice.Id.ShouldNotBe(Guid.Empty);
            invoice.OrderId.ShouldBe(orderId);
            invoice.StorageUrl.ShouldBe(storageUrl);
            invoice.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
            invoice.CreatedAt.ShouldBeLessThanOrEqualTo(afterCreation);
        }

        [Fact]
        public void Rehydrate_ShouldUseProvidedState()
        {
            // Arrange
            var id = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            const string storageUrl = "s3://invoices/2026/04/invoice-456.pdf";
            var createdAt = new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc);

            // Act
            var invoice = Invoice.Rehydrate(id, orderId, storageUrl, createdAt);

            // Assert
            invoice.Id.ShouldBe(id);
            invoice.OrderId.ShouldBe(orderId);
            invoice.StorageUrl.ShouldBe(storageUrl);
            invoice.CreatedAt.ShouldBe(createdAt);
        }
    }
}
