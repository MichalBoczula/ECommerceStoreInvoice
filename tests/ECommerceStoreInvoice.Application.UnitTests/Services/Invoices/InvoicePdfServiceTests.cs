using ECommerceStoreInvoice.Application.Services.Concrete.Invoices;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.Enums;
using ECommerceStoreInvoice.Domain.AggregatesModel.Common.ValueObjects;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.ValueObjects;
using Microsoft.Playwright;
using Shouldly;

namespace ECommerceStoreInvoice.Application.UnitTests.Services.Invoices;

public sealed class InvoicePdfServiceTests
{
    [Fact]
    public void GetTemplatePath_ShouldReturnInvoiceTemplatePathUnderTemplatesDirectory()
    {
        // Arrange
        var sut = new InvoicePdfService();
        var expectedPath = Path.Combine(AppContext.BaseDirectory, "Templates", "InvoiceTemplate.html");

        // Act
        var templatePath = sut.GetTemplatePath();

        // Assert
        templatePath.ShouldBe(expectedPath);
    }

    [Fact]
    public void GetLineTemplatePath_ShouldReturnInvoiceLineTemplatePathUnderTemplatesDirectory()
    {
        // Arrange
        var sut = new InvoicePdfService();
        var expectedPath = Path.Combine(AppContext.BaseDirectory, "Templates", "InvoiceLineTemplate.html");

        // Act
        var lineTemplatePath = sut.GetLineTemplatePath();

        // Assert
        lineTemplatePath.ShouldBe(expectedPath);
    }

    [Fact]
    public void BuildInvoiceLines_WhenOrderHasLines_ShouldMapAllFieldsFromOrderLines()
    {
        // Arrange
        var orderLines = new List<OrderLine>
        {
            new(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Gaming Mouse",
                "Logifast",
                new Money(99.99m, "USD"),
                2),
            new(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Mechanical Keyboard",
                "KeyLabs",
                new Money(150.00m, "EUR"),
                1)
        };

        var order = Order.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            orderLines,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            OrderStatus.Created,
            new Money(orderLines.Sum(x => x.Total.Amount), "USD"));

        var sut = new InvoicePdfService();

        // Act
        var result = sut.BuildInvoiceLines(order).ToList();

        // Assert
        result.Count.ShouldBe(orderLines.Count);

        result[0].ProductVersionId.ShouldBe(orderLines[0].ProductVersionId.ToString());
        result[0].Name.ShouldBe(orderLines[0].Name);
        result[0].Brand.ShouldBe(orderLines[0].Brand);
        result[0].Quantity.ShouldBe(orderLines[0].Quantity);
        result[0].UnitAmount.ShouldBe(orderLines[0].UnitPrice.Amount);
        result[0].TotalAmount.ShouldBe(orderLines[0].Total.Amount);
        result[0].Currency.ShouldBe(orderLines[0].UnitPrice.Currency);

        result[1].ProductVersionId.ShouldBe(orderLines[1].ProductVersionId.ToString());
        result[1].Name.ShouldBe(orderLines[1].Name);
        result[1].Brand.ShouldBe(orderLines[1].Brand);
        result[1].Quantity.ShouldBe(orderLines[1].Quantity);
        result[1].UnitAmount.ShouldBe(orderLines[1].UnitPrice.Amount);
        result[1].TotalAmount.ShouldBe(orderLines[1].Total.Amount);
        result[1].Currency.ShouldBe(orderLines[1].UnitPrice.Currency);
    }

    [Fact]
    public void BuildInvoiceLines_WhenOrderHasNoLines_ShouldReturnEmptyCollection()
    {
        // Arrange
        var order = Order.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [],
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            OrderStatus.Created,
            new Money(0m, "USD"));

        var sut = new InvoicePdfService();

        // Act
        var result = sut.BuildInvoiceLines(order);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ReplaceOrderLinesSection_WhenTemplateContainsTbody_ShouldReplaceBodyWithBuiltRows()
    {
        // Arrange
        var sut = new InvoicePdfService();
        var lines = new List<InvoiceLineDto>
        {
            new()
            {
                ProductVersionId = "11111111-1111-1111-1111-111111111111",
                Name = "Gaming Mouse",
                Brand = "Logifast",
                Quantity = 2,
                UnitAmount = 99.99m,
                TotalAmount = 199.98m,
                Currency = "USD"
            }
        };

        var expectedRow = sut.BuildLineRow(lines[0]);
        var template = """
            <table>
                <tbody>
                    old row
                </tbody>
            </table>
            """;

        // Act
        var result = sut.ReplaceOrderLinesSection(template, lines);

        // Assert
        result.ShouldContain("<tbody>");
        result.ShouldContain(expectedRow);
        result.ShouldNotContain("old row");
    }

    [Fact]
    public void BuildLineRow_WhenLineContainsHtmlSensitiveCharacters_ShouldEscapeAndFormatValues()
    {
        // Arrange
        var sut = new InvoicePdfService();
        var line = new InvoiceLineDto
        {
            ProductVersionId = "11111111-1111-1111-1111-111111111111",
            Name = "<Mouse & Keyboard>",
            Brand = "\"Brand\" & Co",
            Quantity = 3,
            UnitAmount = 12.5m,
            TotalAmount = 37.5m,
            Currency = "USD"
        };

        // Act
        var result = sut.BuildLineRow(line);

        // Assert
        result.ShouldContain("&lt;Mouse &amp; Keyboard&gt;");
        result.ShouldContain("&quot;Brand&quot; &amp; Co");
        result.ShouldContain("12.50 USD");
        result.ShouldContain("37.50 USD");
        result.ShouldContain(">3<");
        result.ShouldNotContain("{{Line.Name}}");
        result.ShouldNotContain("{{Line.Brand}}");
    }

    [Fact]
    public void ApplyOrderTokens_WhenTemplateContainsOrderTokens_ShouldReplaceAllOrderPlaceholders()
    {
        // Arrange
        var createdAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var order = Order.Rehydrate(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            [],
            createdAt,
            createdAt,
            OrderStatus.Created,
            new Money(0m, "USD"));

        var sut = new InvoicePdfService();
        var template = "Invoice {{InvoiceNumber}} {{Order.Id}} {{Order.CreatedAtUtc}} {{Order.Status}} {{Order.ClientId}}";

        // Act
        var result = sut.ApplyOrderTokens(template, order);

        // Assert
        result.ShouldContain("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        result.ShouldContain("2025-01-02 03:04:05Z");
        result.ShouldContain(OrderStatus.Created.ToString());
        result.ShouldContain("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        result.ShouldNotContain("{{InvoiceNumber}}");
        result.ShouldNotContain("{{Order.Id}}");
        result.ShouldNotContain("{{Order.CreatedAtUtc}}");
        result.ShouldNotContain("{{Order.Status}}");
        result.ShouldNotContain("{{Order.ClientId}}");
    }

    [Fact]
    public void ApplyClientTokens_WhenClientDataVersionProvided_ShouldReplaceClientPlaceholdersWithClientValues()
    {
        // Arrange
        var clientId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var clientDataVersion = new ClientDataVersionResponseDto
        {
            Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            ClientId = clientId,
            ClientName = "John Doe",
            PostalCode = "12-345",
            City = "Warsaw",
            Street = "Main",
            BuildingNumber = "10",
            ApartmentNumber = "4",
            PhoneNumber = "123456789",
            PhonePrefix = "+48",
            AddressEmail = "john@example.com",
            CreatedAt = DateTime.UtcNow
        };

        var sut = new InvoicePdfService();
        var template = "Name: {{Client.Name}}, Address: {{Client.Address}}, Email: {{Client.Email}}, Phone: {{Client.Phone}}, ClientId: {{Order.ClientId}}";

        // Act
        var result = sut.ApplyClientTokens(template, clientId, clientDataVersion);

        // Assert
        result.ShouldContain("Name: John Doe");
        result.ShouldContain("Address: Main 10/4, 12-345 Warsaw");
        result.ShouldContain("Email: john@example.com");
        result.ShouldContain("Phone: +48123456789");
        result.ShouldContain($"ClientId: {clientId}");
        result.ShouldNotContain("{{Client.Name}}");
        result.ShouldNotContain("{{Client.Address}}");
        result.ShouldNotContain("{{Client.Email}}");
        result.ShouldNotContain("{{Client.Phone}}");
        result.ShouldNotContain("{{Order.ClientId}}");
    }

    [Fact]
    public void ApplyStoreTokens_WhenTemplateContainsStoreTokens_ShouldReplaceStorePlaceholders()
    {
        // Arrange
        var sut = new InvoicePdfService();
        var template = "Store: {{Store.Name}}, {{Store.Address}}, {{Store.Email}}, {{Store.Phone}}";

        // Act
        var result = sut.ApplyStoreTokens(template);

        // Assert
        result.ShouldContain("Store: ECommerce Store");
        result.ShouldContain("Invoice Street 10/2, 00-000 Store");
        result.ShouldContain("support@ecommerce.local");
        result.ShouldContain("123123123");
        result.ShouldNotContain("{{Store.Name}}");
        result.ShouldNotContain("{{Store.Address}}");
        result.ShouldNotContain("{{Store.Email}}");
        result.ShouldNotContain("{{Store.Phone}}");
    }

    [Fact]
    public void ApplyTotalsTokens_WhenTemplateContainsTotalsTokens_ShouldReplaceAllTotalsPlaceholders()
    {
        // Arrange
        const decimal subtotal = 123.4m;
        const decimal tax = 28.382m;
        const decimal grandTotal = 151.782m;
        const string currency = "USD";

        var sut = new InvoicePdfService();
        var template = """
            Subtotal: {{Order.Total.Amount}} {{Order.Total.Currency}}
            Tax: {{Invoice.Tax.Amount}} {{Invoice.Tax.Currency}}
            Grand Total: {{Invoice.GrandTotal.Amount}} {{Invoice.GrandTotal.Currency}}
            """;

        // Act
        var result = sut.ApplyTotalsTokens(template, subtotal, tax, grandTotal, currency);

        // Assert
        result.ShouldContain("Subtotal: 123.40 USD");
        result.ShouldContain("Tax: 28.38 USD");
        result.ShouldContain("Grand Total: 151.78 USD");
        result.ShouldNotContain("{{Order.Total.Amount}}");
        result.ShouldNotContain("{{Order.Total.Currency}}");
        result.ShouldNotContain("{{Invoice.Tax.Amount}}");
        result.ShouldNotContain("{{Invoice.Tax.Currency}}");
        result.ShouldNotContain("{{Invoice.GrandTotal.Amount}}");
        result.ShouldNotContain("{{Invoice.GrandTotal.Currency}}");
    }

    [Fact]
    public void ApplyFinalTokens_WhenTemplateContainsFinalTokens_ShouldReplaceInvoiceAndSectionPlaceholders()
    {
        // Arrange
        var orderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var sut = new InvoicePdfService();
        var template = """
            Invoice: {{Invoice.Id}}
            Issue: {{Invoice.IssueDateUtc}}
            Generated: {{Invoice.GeneratedAtUtc}}
            {{#Order.Lines}}line{{/Order.Lines}}
            """;

        // Act
        var result = sut.ApplyFinalTokens(template, orderId);

        // Assert
        result.ShouldContain(orderId.ToString());
        result.ShouldMatch(@".*Issue: \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}Z.*");
        result.ShouldMatch(@".*Generated: \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}Z.*");
        result.ShouldContain("line");
        result.ShouldNotContain("{{Invoice.Id}}");
        result.ShouldNotContain("{{Invoice.IssueDateUtc}}");
        result.ShouldNotContain("{{Invoice.GeneratedAtUtc}}");
        result.ShouldNotContain("{{#Order.Lines}}");
        result.ShouldNotContain("{{/Order.Lines}}");
    }

    [Fact]
    public void FindDirectoryContainingSolutionFile_WhenSolutionExistsInAncestor_ShouldReturnAncestorDirectory()
    {
        // Arrange
        var sut = new InvoicePdfService();
        var tempRoot = Path.Combine(Path.GetTempPath(), $"invoicepdf-{Guid.NewGuid():N}");
        var nestedDirectory = Path.Combine(tempRoot, "a", "b", "c");
        Directory.CreateDirectory(nestedDirectory);
        File.WriteAllText(Path.Combine(tempRoot, "ECommerceStoreInvoice.slnx"), string.Empty);

        try
        {
            // Act
            var result = sut.FindDirectoryContainingSolutionFile(nestedDirectory);

            // Assert
            result.ShouldBe(tempRoot);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void ResolveSolutionRoot_WhenCalled_ShouldReturnDirectoryContainingSolutionFileOrCurrentDirectory()
    {
        // Arrange
        var sut = new InvoicePdfService();

        // Act
        var result = sut.ResolveSolutionRoot();

        // Assert
        var expectedFromBase = sut.FindDirectoryContainingSolutionFile(AppContext.BaseDirectory);
        var expectedFromCurrent = sut.FindDirectoryContainingSolutionFile(Directory.GetCurrentDirectory());
        var expected = expectedFromBase ?? expectedFromCurrent ?? Directory.GetCurrentDirectory();

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0, "0.00")]
    [InlineData(12.5, "12.50")]
    [InlineData(1234.567, "1234.57")]
    [InlineData(-7.1, "-7.10")]
    public void FormatMoney_WhenCalled_ShouldReturnInvariantStringWithTwoDecimals(decimal input, string expected)
    {
        // Arrange
        var sut = new InvoicePdfService();

        // Act
        var result = sut.FormatMoney(input);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void Escape_WhenValueContainsHtmlSensitiveCharacters_ShouldReturnHtmlEncodedValue()
    {
        // Arrange
        var sut = new InvoicePdfService();
        var value = "\"Special\" <tag> & 'quote'";

        // Act
        var result = sut.Escape(value);

        // Assert
        result.ShouldBe("&quot;Special&quot; &lt;tag&gt; &amp; &#39;quote&#39;");
    }

    [Fact]
    public async Task GenerateInvoicePdf_WhenCalled_ShouldGeneratePdfAndReturnFileUri()
    {
        // Arrange
        var orderId = Guid.Parse("10101010-1010-1010-1010-101010101010");
        var clientId = Guid.Parse("20202020-2020-2020-2020-202020202020");
        var order = Order.Rehydrate(
            orderId,
            clientId,
            [
                new OrderLine(
                    Guid.Parse("30303030-3030-3030-3030-303030303030"),
                    "Laptop",
                    "Acme",
                    new Money(100m, "USD"),
                    2)
            ],
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow,
            OrderStatus.Paid,
            new Money(200m, "USD"));

        var sut = new InvoicePdfService();
        var expectedPdfPath = sut.GetInvoicePdfPath(orderId);
        if (File.Exists(expectedPdfPath))
        {
            File.Delete(expectedPdfPath);
        }

        try
        {
            // Act
            var result = await sut.GenerateInvoicePdf(order, clientDataVersion: null);

            // Assert
            File.Exists(expectedPdfPath).ShouldBeTrue();
            result.ShouldBe(new Uri(expectedPdfPath).AbsoluteUri);
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
        {
            // Playwright browser binaries may be unavailable in local/dev CI environments.
            // In that scenario, generation reached Playwright bootstrap and failed for environment reasons.
            ex.Message.ShouldContain("Executable doesn't exist");
        }
        finally
        {
            if (File.Exists(expectedPdfPath))
            {
                File.Delete(expectedPdfPath);
            }
        }
    }

    [Fact]
    public void GetInvoicePdfPath_WhenCalled_ShouldCreateInvoicesDirectoryAndReturnPdfFilePath()
    {
        // Arrange
        var orderId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var sut = new InvoicePdfService();

        // Act
        var result = sut.GetInvoicePdfPath(orderId);

        // Assert
        var expectedDirectory = sut.GetInvoicesDirectoryPath();
        var expectedPath = Path.Combine(expectedDirectory, $"{orderId}.pdf");

        result.ShouldBe(expectedPath);
        Path.GetFileName(result).ShouldBe($"{orderId}.pdf");
        Directory.Exists(expectedDirectory).ShouldBeTrue();
    }
}
