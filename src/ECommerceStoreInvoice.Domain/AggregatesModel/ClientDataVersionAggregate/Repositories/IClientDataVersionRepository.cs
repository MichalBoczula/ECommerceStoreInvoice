namespace ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.Repositories
{
    public interface IClientDataVersionRepository
    {
        Task<ClientDataVersion?> GetByClientId(Guid clientId);
        Task Create(ClientDataVersion clientDataVersion);
    }
}
