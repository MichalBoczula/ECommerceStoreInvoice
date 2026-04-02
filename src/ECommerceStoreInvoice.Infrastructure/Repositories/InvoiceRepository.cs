using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
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
            var invoiceDocument = await _context.Invoices
                .Find(x => x.Id == invoiceId)
                .FirstOrDefaultAsync();

            if (invoiceDocument is null)
                return null;

            return InvoiceMapping.MapToDomain(invoiceDocument);
        }

        public async Task<Invoice?> GetInvoiceByOrderId(Guid orderId)
        {
            var invoiceDocument = await _context.Invoices
                .Find(x => x.OrderId == orderId)
                .FirstOrDefaultAsync();

            if (invoiceDocument is null)
                return null;

            return InvoiceMapping.MapToDomain(invoiceDocument);
        }
    }
}
