using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using ECommerceStoreInvoice.Infrastructure.Persistence.Invoices;
using MongoDB.Driver;

namespace ECommerceStoreInvoice.Infrastructure.Repositories
{
    internal sealed class InvoiceRepository(MongoDbContext context) : IInvoiceRepository
    {
        private readonly MongoDbContext _context = context;

        public async Task<Invoice> CreateInvoice(Invoice invoice)
        {
            var invoiceDocument = InvoiceMapping.MapToDocument(invoice);

            await _context.Invoices.InsertOneAsync(invoiceDocument);

            return invoice;
        }

        public async Task<Invoice?> GetInvoiceById(Guid invoiceId)
        {
            var filter = Builders<InvoiceDocument>.Filter;
            var invoiceDocument = await _context.Invoices
                .Find(filter.And(filter.Eq(x => x.Id, invoiceId),
                    filter.Or(filter.Eq(x => x.GenerationStatus, "Completed"),
                        filter.Exists(x => x.GenerationStatus, false))))
                .FirstOrDefaultAsync();

            if (invoiceDocument is null)
                return null;

            return InvoiceMapping.MapToDomain(invoiceDocument);
        }

        public async Task<Invoice?> GetInvoiceByOrderId(Guid orderId)
        {
            var filter = Builders<InvoiceDocument>.Filter;
            var invoiceDocument = await _context.Invoices
                .Find(filter.And(filter.Eq(x => x.OrderId, orderId),
                    filter.Or(filter.Eq(x => x.GenerationStatus, "Completed"),
                        filter.Exists(x => x.GenerationStatus, false))))
                .FirstOrDefaultAsync();

            if (invoiceDocument is null)
                return null;

            return InvoiceMapping.MapToDomain(invoiceDocument);
        }
    }
}
