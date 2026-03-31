using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;

namespace ECommerceStoreInvoice.Application.Services.Abstract
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrder(CreateOrderRequestDto request);
        Task<OrderResponseDto> GetOrderByOrderId(Guid orderId);
    }
}
