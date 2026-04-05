using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;

namespace ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions
{
    public interface IClientDataVersionService
    {
        Task<ClientDataVersionResponseDto> Create(Guid clientId, CreateClientDataVersionRequestDto request);
        Task<ClientDataVersionResponseDto?> GetByClientId(Guid clientId);
    }
}
