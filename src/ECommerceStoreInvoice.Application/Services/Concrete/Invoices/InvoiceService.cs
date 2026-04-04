using System.Globalization;
using System.Net;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Descriptors.Invoices;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate.Repositories;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate;
using ECommerceStoreInvoice.Domain.AggregatesModel.ShoppingCartAggregate.Repositories;
using Microsoft.Playwright;

namespace ECommerceStoreInvoice.Application.Services.Concrete.Invoices
{
    internal sealed class InvoiceService(
        IInvoiceRepository invoiceRepository,
        IOrderRepository orderRepository,
        IShoppingCartRepository shoppingCartRepository)
        : IInvoiceService
    {
        public async Task<InvoiceResponseDto> CreateInvoiceForOrder(Guid orderId)
        {
            var descriptor = new CreateInvoiceForOrderDescriptor();

            var order = await descriptor.LoadOrder(orderId, orderRepository);
            descriptor.ThrowNotFoundExceptionIfOrderMissing(orderId, order);

            var existingInvoice = await descriptor.LoadInvoiceByOrderId(orderId, invoiceRepository);
            descriptor.ThrowAlreadyExistsExceptionIfInvoiceAlreadyExists(orderId, existingInvoice);

            var shoppingCart = await shoppingCartRepository.GetShoppingCartByClientId(order!.ClientId);
            var storageUrl = await GenerateInvoiceFromShoppingCart(order!, shoppingCart);

            var invoice = descriptor.CreateInvoice(orderId, storageUrl);
            var createdInvoice = await descriptor.SaveInvoice(invoice, invoiceRepository);

            return descriptor.MapToResponse(createdInvoice);
        }

        public async Task<InvoiceResponseDto> GetInvoiceById(Guid invoiceId)
        {
            var descriptor = new GetInvoiceByIdDescriptor();

            var invoice = await descriptor.LoadInvoiceById(invoiceId, invoiceRepository);
            descriptor.ThrowNotFoundExceptionIfInvoiceMissing(invoiceId, invoice);

            return descriptor.MapToResponse(invoice!);
        }

        private static async Task<string> GenerateInvoiceFromShoppingCart(Order order, ShoppingCart? shoppingCart)
        {
            var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "InvoiceTemplate.html");
            var template = await File.ReadAllTextAsync(templatePath);

            var linesSource = shoppingCart?.Lines.Any() == true
                ? shoppingCart!.Lines.Select((line, index) => new InvoiceLine(
                    ProductVersionId: index + 1,
                    line.Name,
                    line.Brand,
                    line.Quantity,
                    line.UnitPrice.Amount,
                    line.UnitPrice.Currency,
                    line.Total.Amount))
                : order.Lines.Select(line => new InvoiceLine(
                    ProductVersionId: line.ProductVersionId,
                    line.Name,
                    line.Brand,
                    line.Quantity,
                    line.UnitPrice.Amount,
                    line.UnitPrice.Currency,
                    line.Total.Amount));

            var lines = linesSource.ToList();
            var currency = lines.FirstOrDefault().Currency ?? order.Total.Currency;
            var subtotal = lines.Sum(x => x.TotalAmount);
            var tax = Math.Round(subtotal * 0.23m, 2);
            var grandTotal = subtotal + tax;

            var lineRows = string.Join(Environment.NewLine, lines.Select(line => $@"
                    <tr>
                        <td>
                            <div><strong>{Escape(line.Name)}</strong></div>
                            <div class=\"muted\">Product version: {Escape(line.ProductVersionId.ToString())}</div>
                        </td>
                        <td>{Escape(line.Brand)}</td>
                        <td class=\"text-right\">{line.Quantity}</td>
                        <td class=\"text-right\">{FormatMoney(line.UnitAmount)} {Escape(line.Currency)}</td>
                        <td class=\"text-right\">{FormatMoney(line.TotalAmount)} {Escape(line.Currency)}</td>
                    </tr>"));

            var populatedTemplate = template
                .Replace("{{InvoiceNumber}}", order.Id.ToString())
                .Replace("{{Order.Id}}", order.Id.ToString())
                .Replace("{{Invoice.Id}}", Guid.NewGuid().ToString())
                .Replace("{{Invoice.IssueDateUtc}}", DateTime.UtcNow.ToString("u"))
                .Replace("{{Order.CreatedAtUtc}}", order.CreatedAt.ToString("u"))
                .Replace("{{Order.Status}}", order.Status.ToString())
                .Replace("{{Client.Name}}", "Shopping cart client")
                .Replace("{{Order.ClientId}}", order.ClientId.ToString())
                .Replace("{{Client.Email}}", "unknown@example.com")
                .Replace("{{Client.Phone}}", "n/a")
                .Replace("{{Store.Name}}", "ECommerce Store")
                .Replace("{{Store.AddressLine}}", "Online")
                .Replace("{{Store.City}}", "N/A")
                .Replace("{{Store.Country}}", "N/A")
                .Replace("{{Store.Email}}", "support@ecommerce.local")
                .Replace("{{#Order.Lines}}", string.Empty)
                .Replace("{{/Order.Lines}}", string.Empty)
                .Replace("{{Order.Total.Amount}}", FormatMoney(subtotal))
                .Replace("{{Order.Total.Currency}}", currency)
                .Replace("{{Invoice.Tax.Amount}}", FormatMoney(tax))
                .Replace("{{Invoice.Tax.Currency}}", currency)
                .Replace("{{Invoice.GrandTotal.Amount}}", FormatMoney(grandTotal))
                .Replace("{{Invoice.GrandTotal.Currency}}", currency)
                .Replace("{{Invoice.GeneratedAtUtc}}", DateTime.UtcNow.ToString("u"));

            populatedTemplate = ReplaceLinesSection(populatedTemplate, lineRows);

            var invoicesDirectory = Path.Combine(AppContext.BaseDirectory, "generated-invoices");
            Directory.CreateDirectory(invoicesDirectory);
            var invoicePath = Path.Combine(invoicesDirectory, $"{order.Id}.pdf");

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var page = await browser.NewPageAsync();
            await page.SetContentAsync(populatedTemplate);
            await page.PdfAsync(new PagePdfOptions
            {
                Path = invoicePath,
                Format = "A4",
                PrintBackground = true
            });

            return new Uri(invoicePath).AbsoluteUri;
        }

        private static string ReplaceLinesSection(string template, string lineRows)
        {
            const string startToken = "<tbody>";
            const string endToken = "</tbody>";

            var startIndex = template.IndexOf(startToken, StringComparison.Ordinal);
            var endIndex = template.IndexOf(endToken, StringComparison.Ordinal);

            if (startIndex == -1 || endIndex == -1 || endIndex < startIndex)
            {
                return template;
            }

            var bodyStart = startIndex + startToken.Length;
            return template[..bodyStart] + Environment.NewLine + lineRows + Environment.NewLine + template[endIndex..];
        }

        private static string FormatMoney(decimal value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            return WebUtility.HtmlEncode(value);
        }

        private sealed record InvoiceLine(
            object ProductVersionId,
            string Name,
            string Brand,
            int Quantity,
            decimal UnitAmount,
            string Currency,
            decimal TotalAmount);
    }
}
