using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;

namespace ECommerceStoreInvoice.Application.Services.Abstract
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrder(Guid clientId);
        Task<OrderResponseDto> GetOrderByOrderId(Guid orderId);
    }
}
