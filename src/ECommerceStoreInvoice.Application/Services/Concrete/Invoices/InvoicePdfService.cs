using ECommerceStoreInvoice.Application.Common.ResponsesDto.Invoices;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using Microsoft.Playwright;
using System.Globalization;
using System.Net;

namespace ECommerceStoreInvoice.Application.Services.Concrete.Invoices
{
    internal sealed class InvoicePdfService : IInvoicePdfService
    {
        private const decimal VatRate = 0.23m;

        public async Task<string> GenerateInvoicePdf(Order order, ShoppingCart? shoppingCart, ClientDataVersionResponseDto? clientDataVersion)
        {
            var templatePath = GetTemplatePath();
            var template = await File.ReadAllTextAsync(templatePath);

            var lines = BuildInvoiceLines(order, shoppingCart);
            var subtotal = lines.Sum(x => x.TotalAmount);
            var currency = lines.FirstOrDefault()?.Currency ?? order.Total.Currency;
            var tax = Math.Round(subtotal * VatRate, 2);
            var grandTotal = subtotal + tax;

            var withRows = ReplaceOrderLinesSection(template, lines);
            var withOrderData = ApplyOrderTokens(withRows, order);
            var withClientData = ApplyClientTokens(withOrderData, order.ClientId, clientDataVersion);
            var withStoreData = ApplyStoreTokens(withClientData);
            var withTotals = ApplyTotalsTokens(withStoreData, subtotal, tax, grandTotal, currency);

            var invoiceHtml = ApplyFinalTokens(withTotals, order.Id);
            var invoicePath = GetInvoicePdfPath(order.Id);

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();
            await page.SetContentAsync(invoiceHtml);
            await page.PdfAsync(new PagePdfOptions
            {
                Path = invoicePath,
                Format = "A4",
                PrintBackground = true
            });

            return new Uri(invoicePath).AbsoluteUri;
        }

        internal string GetTemplatePath()
        {
            return Path.Combine(AppContext.BaseDirectory, "Templates", "InvoiceTemplate.html");
        }

        internal string GetLineTemplatePath()
        {
            return Path.Combine(AppContext.BaseDirectory, "Templates", "InvoiceLineTemplate.html");
        }

        internal IReadOnlyCollection<InvoiceLineDto> BuildInvoiceLines(Order order, ShoppingCart? shoppingCart)
        {
            if (shoppingCart?.Lines.Any() == true)
            {
                return shoppingCart.Lines
                    .Select((line, index) => new InvoiceLineDto
                    {
                        ProductVersionId = (index + 1).ToString(CultureInfo.InvariantCulture),
                        Name = line.Name,
                        Brand = line.Brand,
                        Quantity = line.Quantity,
                        UnitAmount = line.UnitPrice.Amount,
                        TotalAmount = line.Total.Amount,
                        Currency = line.UnitPrice.Currency
                    })
                    .ToList();
            }

            return order.Lines.Select(line => new InvoiceLineDto
            {
                ProductVersionId = line.ProductVersionId.ToString(),
                Name = line.Name,
                Brand = line.Brand,
                Quantity = line.Quantity,
                UnitAmount = line.UnitPrice.Amount,
                TotalAmount = line.Total.Amount,
                Currency = line.UnitPrice.Currency
            }).ToList();
        }

        internal string ReplaceOrderLinesSection(string template, IReadOnlyCollection<InvoiceLineDto> lines)
        {
            var rows = string.Join(Environment.NewLine, lines.Select(BuildLineRow));

            var startToken = "<tbody>";
            var endToken = "</tbody>";
            var startIndex = template.IndexOf(startToken, StringComparison.Ordinal);
            var endIndex = template.IndexOf(endToken, StringComparison.Ordinal);

            if (startIndex == -1 || endIndex == -1 || endIndex < startIndex)
            {
                return template;
            }

            var bodyStart = startIndex + startToken.Length;
            return template[..bodyStart] + Environment.NewLine + rows + Environment.NewLine + template[endIndex..];
        }

        internal string BuildLineRow(InvoiceLineDto line)
        {
            var lineTemplate = File.ReadAllText(GetLineTemplatePath());

            return lineTemplate
                .Replace("{{Line.Name}}", Escape(line.Name))
                .Replace("{{Line.ProductVersionId}}", Escape(line.ProductVersionId))
                .Replace("{{Line.Brand}}", Escape(line.Brand))
                .Replace("{{Line.Quantity}}", line.Quantity.ToString(CultureInfo.InvariantCulture))
                .Replace("{{Line.UnitAmount}}", FormatMoney(line.UnitAmount))
                .Replace("{{Line.TotalAmount}}", FormatMoney(line.TotalAmount))
                .Replace("{{Line.Currency}}", Escape(line.Currency));
        }

        internal string ApplyOrderTokens(string template, Order order)
        {
            return template
                .Replace("{{InvoiceNumber}}", order.Id.ToString())
                .Replace("{{Order.Id}}", order.Id.ToString())
                .Replace("{{Order.CreatedAtUtc}}", order.CreatedAt.ToString("u"))
                .Replace("{{Order.Status}}", order.Status.ToString())
                .Replace("{{Order.ClientId}}", order.ClientId.ToString());
        }

        internal string ApplyClientTokens(string template, Guid clientId, ClientDataVersionResponseDto? clientDataVersion)
        {
            return template
                .Replace("{{Client.Name}}", $"Client {clientId}")
                .Replace("{{Client.Email}}", clientDataVersion?.AddressEmail ?? "unknown@example.com")
                .Replace("{{Client.Phone}}", clientDataVersion is null ? "n/a" : $"{clientDataVersion.PhonePrefix}{clientDataVersion.PhoneNumber}")
                .Replace("{{Order.ClientId}}", clientId.ToString());
        }

        internal string ApplyStoreTokens(string template)
        {
            return template
                .Replace("{{Store.Name}}", "ECommerce Store")
                .Replace("{{Store.AddressLine}}", "Online")
                .Replace("{{Store.City}}", "N/A")
                .Replace("{{Store.Country}}", "N/A")
                .Replace("{{Store.Email}}", "support@ecommerce.local");
        }

        internal string ApplyTotalsTokens(string template, decimal subtotal, decimal tax, decimal grandTotal, string currency)
        {
            return template
                .Replace("{{Order.Total.Amount}}", FormatMoney(subtotal))
                .Replace("{{Order.Total.Currency}}", currency)
                .Replace("{{Invoice.Tax.Amount}}", FormatMoney(tax))
                .Replace("{{Invoice.Tax.Currency}}", currency)
                .Replace("{{Invoice.GrandTotal.Amount}}", FormatMoney(grandTotal))
                .Replace("{{Invoice.GrandTotal.Currency}}", currency);
        }

        internal string ApplyFinalTokens(string template, Guid orderId)
        {
            return template
                .Replace("{{Invoice.Id}}", orderId.ToString())
                .Replace("{{Invoice.IssueDateUtc}}", DateTime.UtcNow.ToString("u"))
                .Replace("{{Invoice.GeneratedAtUtc}}", DateTime.UtcNow.ToString("u"))
                .Replace("{{#Order.Lines}}", string.Empty)
                .Replace("{{/Order.Lines}}", string.Empty);
        }

        internal string GetInvoicePdfPath(Guid orderId)
        {
            var directory = GetInvoicesDirectoryPath();
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, $"{orderId}.pdf");
        }

        internal string GetInvoicesDirectoryPath()
        {
            var solutionRoot = ResolveSolutionRoot();
            return Path.Combine(solutionRoot, "Invoices");
        }

        internal string ResolveSolutionRoot()
        {
            var fromBase = FindDirectoryContainingSolutionFile(AppContext.BaseDirectory);
            if (fromBase is not null)
            {
                return fromBase;
            }

            var fromCurrent = FindDirectoryContainingSolutionFile(Directory.GetCurrentDirectory());
            if (fromCurrent is not null)
            {
                return fromCurrent;
            }

            return Directory.GetCurrentDirectory();
        }

        internal string? FindDirectoryContainingSolutionFile(string startDirectory)
        {
            var directory = new DirectoryInfo(startDirectory);
            while (directory is not null)
            {
                var solutionFile = Path.Combine(directory.FullName, "ECommerceStoreInvoice.slnx");
                if (File.Exists(solutionFile))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return null;
        }

        internal string FormatMoney(decimal value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        internal string Escape(string value)
        {
            return WebUtility.HtmlEncode(value);
        }
    }
}
