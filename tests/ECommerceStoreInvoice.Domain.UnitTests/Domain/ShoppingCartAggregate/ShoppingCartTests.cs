using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.ValueObjects;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Domain.ShoppingCartAggregate
{
    public class ShoppingCartTests
    {
        [Fact]
        public void Ctor_ShouldInitializeEmptyCart()
        {
            // Arrange
            var clientId = Guid.NewGuid();

            // Act
            var cart = new ShoppingCart(clientId);

            // Assert
            cart.Id.ShouldNotBe(Guid.Empty);
            cart.ClientId.ShouldBe(clientId);
            cart.Lines.ShouldBeEmpty();
            cart.UpdatedAt.ShouldBeGreaterThanOrEqualTo(cart.CreatedAt);
        }

        [Fact]
        public void ReplaceLines_ShouldReplaceAllLines()
        {
            // Arrange
            var cart = new ShoppingCart(Guid.NewGuid());
            var productAId = Guid.NewGuid();
            var productBId = Guid.NewGuid();
            var lines = new[]
            {
                new ShoppingCartLine(productAId, 2),
                new ShoppingCartLine(productBId, 3)
            };

            // Act
            cart.ReplaceLines(lines);

            // Assert
            cart.Lines.Count.ShouldBe(2);
            cart.Lines.ShouldContain(x => x.ProductId == productAId && x.Quantity == 2);
            cart.Lines.ShouldContain(x => x.ProductId == productBId && x.Quantity == 3);
        }

        [Fact]
        public void Clear_ShouldRemoveAllLines()
        {
            // Arrange
            var cart = new ShoppingCart(Guid.NewGuid());
            cart.ReplaceLines([
                new ShoppingCartLine(Guid.NewGuid(), 1)
            ]);

            // Act
            cart.Clear();

            // Assert
            cart.Lines.ShouldBeEmpty();
        }

        [Fact]
        public void Rehydrate_ShouldRestoreStateCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var createdAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var updatedAt = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc);
            var lines = new[]
            {
                new ShoppingCartLine(productId, 5)
            };

            // Act
            var cart = ShoppingCart.Rehydrate(id, clientId, createdAt, updatedAt, lines);

            // Assert
            cart.Id.ShouldBe(id);
            cart.ClientId.ShouldBe(clientId);
            cart.CreatedAt.ShouldBe(createdAt);
            cart.UpdatedAt.ShouldBe(updatedAt);
            cart.Lines.Count.ShouldBe(1);
            cart.Lines.First().ProductId.ShouldBe(productId);
            cart.Lines.First().Quantity.ShouldBe(5);
        }

        [Fact]
        public void ShoppingCartLine_ChangeQuantity_ShouldUpdateQuantity()
        {
            // Arrange
            var line = new ShoppingCartLine(Guid.NewGuid(), 2);

            // Act
            line.ChangeQuantity(10);

            // Assert
            line.Quantity.ShouldBe(10);
        }
    }
}