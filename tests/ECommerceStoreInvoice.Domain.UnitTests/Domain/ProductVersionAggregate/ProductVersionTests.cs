using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.ProductVersionAggregate;
using Shouldly;

namespace ECommerceStoreInvoice.Domain.UnitTests.Domain.ProductVersionAggregate
{
    public class ProductVersionTests
    {
        [Fact]
        public void Ctor_ShouldInitializeAsActiveWithCurrentTimestamp()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var price = new Money(19.99m, "USD");

            // Act
            var productVersion = new ProductVersion(productId, price, "Keyboard", "Logi");

            // Assert
            productVersion.Id.ShouldNotBe(Guid.Empty);
            productVersion.IsActive.ShouldBeTrue();
            productVersion.DeactivatedAt.ShouldBeNull();
            productVersion.ProductId.ShouldBe(productId);
            productVersion.Price.ShouldBe(price);
            productVersion.Name.ShouldBe("Keyboard");
            productVersion.Brand.ShouldBe("Logi");
            productVersion.CreatedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        }

        [Fact]
        public void Deactivate_WhenActive_ShouldSetInactiveAndDeactivatedAt()
        {
            // Arrange
            var productVersion = new ProductVersion(Guid.NewGuid(), new Money(10, "USD"), "Mouse", "BrandA");

            // Act
            productVersion.Deactivate();

            // Assert
            productVersion.IsActive.ShouldBeFalse();
            productVersion.DeactivatedAt.ShouldNotBeNull();
        }

        [Fact]
        public void Deactivate_WhenAlreadyInactive_ShouldNotChangeDeactivatedAt()
        {
            // Arrange
            var productVersion = new ProductVersion(Guid.NewGuid(), new Money(10, "USD"), "Mouse", "BrandA");
            productVersion.Deactivate();
            var firstDeactivatedAt = productVersion.DeactivatedAt;

            // Act
            productVersion.Deactivate();

            // Assert
            productVersion.IsActive.ShouldBeFalse();
            productVersion.DeactivatedAt.ShouldBe(firstDeactivatedAt);
        }

        [Fact]
        public void Rehydrate_ShouldUseProvidedState()
        {
            // Arrange
            var id = Guid.NewGuid();
            var createdAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var deactivatedAt = new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);
            var productId = Guid.NewGuid();
            var price = new Money(99.99m, "USD");

            // Act
            var productVersion = ProductVersion.Rehydrate(
                id,
                false,
                createdAt,
                deactivatedAt,
                productId,
                price,
                "Monitor",
                "BrandB");

            // Assert
            productVersion.Id.ShouldBe(id);
            productVersion.IsActive.ShouldBeFalse();
            productVersion.CreatedAt.ShouldBe(createdAt);
            productVersion.DeactivatedAt.ShouldBe(deactivatedAt);
            productVersion.ProductId.ShouldBe(productId);
            productVersion.Price.ShouldBe(price);
            productVersion.Name.ShouldBe("Monitor");
            productVersion.Brand.ShouldBe("BrandB");
        }
    }
}
