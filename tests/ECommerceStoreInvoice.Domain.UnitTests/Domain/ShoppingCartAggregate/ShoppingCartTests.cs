using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Domain.ShoppingCartAggregate
{
    public class ShoppingCartTests
    {
        [Fact]
        public void Ctor_ShouldInitializeEmptyCartWithDefaultUsdTotal()
        {
            // Arrange
            var clientId = Guid.NewGuid();

            // Act
            var cart = new ShoppingCart(clientId);

            // Assert
            cart.ClientId.ShouldBe(clientId);
            cart.Lines.ShouldBeEmpty();
            cart.Total.Amount.ShouldBe(0);
            cart.Total.Currency.ShouldBe("USD");
            cart.UpdatedAt.ShouldBeGreaterThanOrEqualTo(cart.CreatedAt);
        }

        [Fact]
        public void ReplaceLines_ShouldReplaceAllLinesAndRecalculateTotal()
        {
            // Arrange
            var cart = new ShoppingCart(Guid.NewGuid());
            var lines = new[]
            {
                new ShoppingCartLine("Mouse", "BrandA", new Money(100, "USD"), 2),
                new ShoppingCartLine("Keyboard", "BrandB", new Money(50, "USD"), 3)
            };

            // Act
            cart.ReplaceLines(lines);

            // Assert
            cart.Lines.Count.ShouldBe(2);
            cart.Total.Amount.ShouldBe(350);
            cart.Total.Currency.ShouldBe("USD");
        }

        [Fact]
        public void Clear_ShouldRemoveAllLinesAndSetTotalToZero()
        {
            // Arrange
            var cart = new ShoppingCart(Guid.NewGuid());
            cart.ReplaceLines([
                new ShoppingCartLine("Mouse", "BrandA", new Money(100, "USD"), 1)
            ]);

            // Act
            cart.Clear();

            // Assert
            cart.Lines.ShouldBeEmpty();
            cart.Total.Amount.ShouldBe(0);
            cart.Total.Currency.ShouldBe("USD");
        }

        [Fact]
        public void Rehydrate_ShouldUseProvidedStateAndRecalculateTotal()
        {
            // Arrange
            var id = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            var createdAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var updatedAt = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc);
            var lines = new[]
            {
                new ShoppingCartLine("Headphones", "BrandC", new Money(75, "eur"), 2)
            };

            // Act
            var cart = ShoppingCart.Rehydrate(id, clientId, createdAt, updatedAt, lines);

            // Assert
            cart.Id.ShouldBe(id);
            cart.ClientId.ShouldBe(clientId);
            cart.CreatedAt.ShouldBe(createdAt);
            cart.UpdatedAt.ShouldBe(updatedAt);
            cart.Lines.Count.ShouldBe(1);
            cart.Total.Amount.ShouldBe(150);
            cart.Total.Currency.ShouldBe("EUR");
        }
    }
}
