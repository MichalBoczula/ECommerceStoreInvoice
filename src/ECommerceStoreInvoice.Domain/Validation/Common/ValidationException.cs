namespace ECommerceStoreInvoice.Domain.Validation.Common
{
    public sealed class ValidationException : Exception
    {
        public ValidationResult ValidationResult { get; private set; }

        public ValidationException(ValidationResult validationResult)
        {
            this.ValidationResult = validationResult;
        }
    }
}
