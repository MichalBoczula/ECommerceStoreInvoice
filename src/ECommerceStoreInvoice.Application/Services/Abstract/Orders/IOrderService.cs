using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;

namespace ECommerceStoreInvoice.Application.Services.Abstract.Orders
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrder(Guid clientId);
        Task<IReadOnlyCollection<OrderResponseDto>> GetOrdersByClientId(Guid clientId);
        Task<OrderResponseDto> GetOrderByOrderId(Guid orderId);
        Task<OrderResponseDto> UpdateOrderStatus(Guid orderId, UpdateOrderStatusRequestDto request);
    }
}
