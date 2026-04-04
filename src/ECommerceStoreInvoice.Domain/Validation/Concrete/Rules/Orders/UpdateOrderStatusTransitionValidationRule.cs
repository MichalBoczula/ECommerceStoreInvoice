using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Orders
{
    internal sealed class UpdateOrderStatusTransitionValidationRule : IValidationRule<(Order order, OrderStatus newStatus)>
    {
        private readonly ValidationError transitionNotAllowed;

        public UpdateOrderStatusTransitionValidationRule()
        {
            transitionNotAllowed = new ValidationError
            {
                Message = "Only transitions from Created to Paid or Cancelled are allowed.",
                Name = nameof(UpdateOrderStatusTransitionValidationRule),
                Entity = nameof(Order)
            };
        }

        public async Task IsValid((Order order, OrderStatus newStatus) entity, ValidationResult validationResults)
        {
            if (entity.order.Status == OrderStatus.Created &&
                (entity.newStatus == OrderStatus.Paid || entity.newStatus == OrderStatus.Cancelled))
            {
                return;
            }

            validationResults.AddValidationError(transitionNotAllowed);
        }

        public List<ValidationError> Describe()
        {
            return [transitionNotAllowed];
        }
    }
}
