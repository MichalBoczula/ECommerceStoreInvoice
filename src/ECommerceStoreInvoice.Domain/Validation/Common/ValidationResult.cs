namespace ECommerceStoreInvoice.Domain.Validation.Common
{
    public sealed class ValidationResult
    {
        public bool IsValid => !_validationErrors.Any();
        public List<ValidationError> _validationErrors { get; } = new();

        public void AddValidationError(ValidationError validationError)
        {
            _validationErrors.Add(validationError);
        }

        public IReadOnlyList<ValidationError> GetValidationErrors() => _validationErrors.AsReadOnly();
    }
}
