using System.Globalization;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using ECommerceStoreInvoice.Domain.Validation.Common;
using ECommerceStoreInvoice.Domain.Validation.Concrete.Policies;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Validation.Policies;

public sealed class BusinessBoundaryPolicyTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-1")]
    public async Task SnapshotPriceRejectsOnlyTheNonpositiveAmount(string amount)
    {
        var product = new ProductVersion(Guid.NewGuid(), new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), "PLN"), "Phone", "Brand");
        var result = await new ProductVersionValidationPolicy().Validate(product);
        ExactErrors(result, ("ProductVersionPriceValidationRule", "Price amount must be greater than zero.", "ProductVersion"));
    }

    [Theory]
    [InlineData("0.0000000000000000000000000001")]
    [InlineData("79228162514264337593543950335")]
    public async Task SnapshotPolicyAcceptsPositiveDecimalBoundaries(string amount)
    {
        var product = new ProductVersion(Guid.NewGuid(), new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), "PLN"), "Phone", "Brand");
        var result = await new ProductVersionValidationPolicy().Validate(product);
        result.IsValid.ShouldBeTrue();
        result.GetValidationErrors().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("usd", "USD")]
    [InlineData("pln", "PLN")]
    [InlineData("uSd", "USD")]
    [InlineData("pLn", "PLN")]
    public async Task MoneyNormalizesSupportedCurrencyBeforePolicyValidation(string input, string saved)
    {
        var product = new ProductVersion(Guid.NewGuid(), new Money(1, input), "Phone", "Brand");
        product.Price.Currency.ShouldBe(saved);
        var result = await new ProductVersionValidationPolicy().Validate(product);
        result.IsValid.ShouldBeTrue();
        result.GetValidationErrors().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("EUR", false)]
    [InlineData(" USD ", false)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    public async Task SnapshotCurrencyReportsTheExactIsolatedOrPairedErrors(string currency, bool required)
    {
        var product = new ProductVersion(Guid.NewGuid(), new Money(1, currency), "Phone", "Brand");
        var result = await new ProductVersionValidationPolicy().Validate(product);
        var expected = new List<(string, string, string)>();
        if (required) expected.Add(("ProductVersionStringsValidationRule", "Price currency cannot be null or whitespace.", "ProductVersion"));
        expected.Add(("ProductVersionStringsValidationRule", "Price currency must be USD or PLN.", "ProductVersion"));
        ExactErrors(result, expected.ToArray());
    }

    [Theory]
    [InlineData("Name", null)]
    [InlineData("Name", "")]
    [InlineData("Name", "   ")]
    [InlineData("Brand", null)]
    [InlineData("Brand", "")]
    [InlineData("Brand", "   ")]
    public async Task SnapshotStringsRejectOneInvalidFieldAtATime(string field, string? value)
    {
        var product = new ProductVersion(Guid.NewGuid(), new Money(1, "PLN"), field == "Name" ? value! : "Phone", field == "Brand" ? value! : "Brand");
        var result = await new ProductVersionValidationPolicy().Validate(product);
        ExactErrors(result, ("ProductVersionStringsValidationRule", $"{field} cannot be null or whitespace.", "ProductVersion"));
    }

    [Fact]
    public async Task GuidCollectionNullIsAStandaloneRequiredCollectionError()
    {
        var result = await new GuidCollectionValidationPolicy().Validate(null!);
        ExactErrors(result, ("GuidCollectionIsEmptyValidationRule", "The list of IDs cannot be empty.", "GuidCollection"));
    }

    [Fact]
    public async Task GuidCollectionAllowsRepeatedNonemptyIds()
    {
        var id = Guid.NewGuid();
        var result = await new GuidCollectionValidationPolicy().Validate([id, id]);
        result.IsValid.ShouldBeTrue();
        result.GetValidationErrors().ShouldBeEmpty();
    }

    private static void ExactErrors(ValidationResult result, params (string, string, string)[] expected)
    {
        result.IsValid.ShouldBeFalse();
        result.GetValidationErrors().Select(error => (error.Name, error.Message, error.Entity)).ShouldBe(expected);
    }
}
