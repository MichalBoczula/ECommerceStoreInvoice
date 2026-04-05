using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.Repositories;
using ECommerceStoreInvoice.Infrastructure.Context;
using ECommerceStoreInvoice.Infrastructure.Mapping;
using MongoDB.Driver;

namespace ECommerceStoreInvoice.Infrastructure.Repositories
{
    internal sealed class ClientDataVersionRepository(MongoDbContext context) : IClientDataVersionRepository
    {
        private readonly MongoDbContext _context = context;

        public async Task Create(ClientDataVersion clientDataVersion)
        {
            var clientDataVersionDocument = ClientDataVersionMapping.MapToDocument(clientDataVersion);
            await _context.ClientDataVersions.InsertOneAsync(clientDataVersionDocument);
        }

        public async Task<ClientDataVersion?> GetByClientId(Guid clientId)
        {
            var clientDataVersionDocument = await _context.ClientDataVersions
                .Find(x => x.ClientId == clientId)
                .SortByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (clientDataVersionDocument is null)
                return null;

            return ClientDataVersionMapping.MapToDomain(clientDataVersionDocument);
        }
    }
}
