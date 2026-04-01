using ECommerceStoreInvoice.Application.Common.RequestsDto.ProductVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;

namespace ECommerceStoreInvoice.Application.Services.Abstract
{
    public interface IProductVersionService
    {
        Task<ProductVersionResponseDto?> GetProductVersionById(Guid id);
        Task<ProductVersionResponseDto> CreateProductVersion(CreateProductVersionRequestDto request);
    }
}
