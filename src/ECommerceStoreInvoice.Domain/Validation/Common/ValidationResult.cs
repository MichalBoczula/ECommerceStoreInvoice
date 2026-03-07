namespace ProductCatalogECommerceStoreInvoice.Domain.Validation.Common
{
    public sealed class ValidationResult
    {
        public bool IsValid => !_validationErrors.Any();
        public List<ValidationError> _validationErrors { get; } = new();

        public void AddValidationError(ValidationError validationError)
        {
            _validationErrors.Add(validationError);
        }

        public IReadOnlyList<ValidationError> GetValidatonErrors() => _validationErrors.AsReadOnly();
    }
}
