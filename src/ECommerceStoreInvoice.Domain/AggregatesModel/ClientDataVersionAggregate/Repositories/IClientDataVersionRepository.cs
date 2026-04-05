namespace ECommerceStoreInvoice.Domain.AggregatesModel.ClientDataVersionAggregate.Repositories
{
    public interface IClientDataVersionRepository
    {
        Task<ClientDataVersion?> GetById(Guid id);
        Task Create(ClientDataVersion clientDataVersion);
    }
}
