using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;

namespace ECommerceStoreInvoice.Domain.Validation.Concrete.Rules.Orders
{
    internal sealed class InvoiceOrderStatusValidationRule : IValidationRule<InvoiceOrderStatusValidationContext>
    {
        private readonly ValidationError orderMustBePaid;

        public InvoiceOrderStatusValidationRule()
        {
            orderMustBePaid = new ValidationError
            {
                Message = "Invoice can be created only for paid orders.",
                Name = nameof(InvoiceOrderStatusValidationRule),
                Entity = nameof(InvoiceOrderStatusValidationContext.Order)
            };
        }

        public async Task IsValid(InvoiceOrderStatusValidationContext entity, ValidationResult validationResults)
        {
            if (entity.Order.Status == OrderStatus.Paid)
            {
                return;
            }

            validationResults.AddValidationError(orderMustBePaid);
        }

        public List<ValidationError> Describe()
        {
            return [orderMustBePaid];
        }
    }
}
