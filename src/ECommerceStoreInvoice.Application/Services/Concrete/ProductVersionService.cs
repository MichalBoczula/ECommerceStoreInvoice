using ECommerceStoreInvoice.Application.Services.Abstract;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate.Repositories;

namespace ECommerceStoreInvoice.Application.Services.Concrete
{
    internal class ProductVersionService(
        IProductVersionRepository _productVersionRepository)
        : IProductVersionService
    {
    }
}
