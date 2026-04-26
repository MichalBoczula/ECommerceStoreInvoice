using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Tests
{
    public sealed class InvoiceRepositoryTests : IClassFixture<MongoDbTestFixture>
    {
        private readonly MongoDbTestFixture _fixture;

        public InvoiceRepositoryTests(MongoDbTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CreateInvoice_ShouldSaveInvoice()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IInvoiceRepository>();

            var invoice = Invoice.Rehydrate(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "https://storage.test/invoices/invoice-1.pdf",
                DateTime.UtcNow);

            // act
            await repository.CreateInvoice(invoice);

            // assert
            var result = await repository.GetInvoiceById(invoice.Id);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(invoice.Id);
            result.OrderId.ShouldBe(invoice.OrderId);
            result.ClientDataVersionId.ShouldBe(invoice.ClientDataVersionId);
            result.StorageUrl.ShouldBe(invoice.StorageUrl);
        }

        [Fact]
        public async Task GetInvoiceById_ShouldReturnInvoice_WhenExists()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IInvoiceRepository>();

            var invoice = Invoice.Rehydrate(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "https://storage.test/invoices/invoice-2.pdf",
                DateTime.UtcNow);

            await repository.CreateInvoice(invoice);

            // act
            var result = await repository.GetInvoiceById(invoice.Id);

            // assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(invoice.Id);
            result.OrderId.ShouldBe(invoice.OrderId);
            result.ClientDataVersionId.ShouldBe(invoice.ClientDataVersionId);
            result.StorageUrl.ShouldBe(invoice.StorageUrl);
        }

        [Fact]
        public async Task GetInvoiceByOrderId_ShouldReturnInvoice_WhenExists()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            var repository = serviceProvider
                .GetRequiredService<IInvoiceRepository>();

            var orderId = Guid.NewGuid();
            var invoice = Invoice.Rehydrate(
                Guid.NewGuid(),
                orderId,
                Guid.NewGuid(),
                "https://storage.test/invoices/invoice-3.pdf",
                DateTime.UtcNow);

            await repository.CreateInvoice(invoice);

            // act
            var result = await repository.GetInvoiceByOrderId(orderId);

            // assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(invoice.Id);
            result.OrderId.ShouldBe(orderId);
            result.ClientDataVersionId.ShouldBe(invoice.ClientDataVersionId);
            result.StorageUrl.ShouldBe(invoice.StorageUrl);
        }
    }
}
