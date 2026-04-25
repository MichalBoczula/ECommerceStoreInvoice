using ECommerceStoreInvoice.Infrastructure.Configuration;
using ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Configuration;
using MongoDB.Driver;
using Shouldly;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Integration.Tests
{
    public sealed class MongoInitializerTests : IClassFixture<MongoDbTestFixture>
    {
        private readonly MongoDbTestFixture _fixture;

        public MongoInitializerTests(MongoDbTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task InitializeInfrastructureAsync_ShouldCreateExpectedIndexes()
        {
            // arrange
            var databaseName = $"invoice-tests-{Guid.NewGuid():N}";

            await using var serviceProvider = TestServiceProviderFactory.Create(
                _fixture.ConnectionString,
                databaseName);

            // act
            await serviceProvider.InitializeInfrastructureAsync();

            // assert
            var client = new MongoClient(_fixture.ConnectionString);
            var database = client.GetDatabase(databaseName);

            var shoppingCartIndexes = await database
                .GetCollection<object>("shopping-carts")
                .Indexes
                .ListAsync();

            var shoppingCartIndexNames = await shoppingCartIndexes
                .ToListAsync();

            shoppingCartIndexNames.ShouldContain(
                x => x["name"] == "UX_ShoppingCart_ClientId");

            var invoiceIndexes = await database
                .GetCollection<object>("invoices")
                .Indexes
                .ListAsync();

            var invoiceIndexNames = await invoiceIndexes
                .ToListAsync();

            invoiceIndexNames.ShouldContain(
                x => x["name"] == "UX_Invoice_OrderId");

            var clientDataVersionIndexes = await database
                .GetCollection<object>("client-data-versions")
                .Indexes
                .ListAsync();

            var clientDataVersionIndexNames = await clientDataVersionIndexes
                .ToListAsync();

            clientDataVersionIndexNames.ShouldContain(
                x => x["name"] == "IX_ClientDataVersion_ClientId_CreatedAtDesc");
        }
    }
}