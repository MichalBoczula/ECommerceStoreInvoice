using ECommerceStoreInvoice.Application.Common.ResponsesDto.Invoices;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using Microsoft.Playwright;
using System.Globalization;
using System.Net;
using System.Diagnostics;

namespace ECommerceStoreInvoice.Application.Services.Concrete.Invoices
{
    internal sealed class InvoicePdfService : IInvoicePdfService, IAsyncDisposable
    {
        private const decimal VatRate = 0.23m;
        private static readonly SemaphoreSlim _initializationLock = new(1, 1);
        private static bool _playwrightInstalled;

        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private string? _cachedMainTemplate;
        private string? _cachedLineTemplate;

        public async Task<string> GenerateInvoicePdf(Order order, ClientDataVersionResponseDto? clientDataVersion)
        {
            await EnsureInitializedAsync();

            var lines = BuildInvoiceLines(order);
            var subtotal = lines.Sum(x => x.TotalAmount);
            var currency = lines.FirstOrDefault()?.Currency ?? order.Total.Currency;
            var tax = Math.Round(subtotal * VatRate, 2);
            var grandTotal = subtotal + tax;

            var withRows = ReplaceOrderLinesSection(_cachedMainTemplate!, lines);
            var withOrderData = ApplyOrderTokens(withRows, order);
            var withClientData = ApplyClientTokens(withOrderData, order.ClientId, clientDataVersion);
            var withStoreData = ApplyStoreTokens(withClientData);
            var withTotals = ApplyTotalsTokens(withStoreData, subtotal, tax, grandTotal, currency);

            var invoiceHtml = ApplyFinalTokens(withTotals, order.Id);
            var invoicePath = GetInvoicePdfPath(order.Id);

            var page = await _browser!.NewPageAsync();
            try
            {
                await page.SetContentAsync(invoiceHtml);
                await page.PdfAsync(new PagePdfOptions
                {
                    Path = invoicePath,
                    Format = "A4",
                    PrintBackground = true
                });
            }
            finally
            {
                await page.CloseAsync();
            }

            return new Uri(invoicePath).AbsoluteUri;
        }

        private async Task EnsureInitializedAsync()
        {
            if (_browser != null && _cachedMainTemplate != null) return;

            await _initializationLock.WaitAsync();
            try
            {
                if (_browser == null)
                {
                    await EnsurePlaywrightInstalledAsync();
                    _playwright = await Playwright.CreateAsync();
                    _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
                }

                if (_cachedMainTemplate == null)
                {
                    _cachedMainTemplate = await File.ReadAllTextAsync(GetTemplatePath());
                    _cachedLineTemplate = await File.ReadAllTextAsync(GetLineTemplatePath());
                }
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        private async Task EnsurePlaywrightInstalledAsync()
        {
            if (_playwrightInstalled) return;

            var scriptPath = FindPlaywrightScriptPath();
            var startInfo = new ProcessStartInfo
            {
                FileName = "pwsh",
                Arguments = $"\"{scriptPath}\" install",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Playwright installation.");
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Playwright installation failed: {error}");
            }

            _playwrightInstalled = true;
        }

        private string FindPlaywrightScriptPath()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var path = Path.Combine(directory.FullName, "playwright.ps1");
                if (File.Exists(path)) return path;
                directory = directory.Parent;
            }
            throw new FileNotFoundException("Could not find playwright.ps1");
        }

        internal string GetTemplatePath() => Path.Combine(AppContext.BaseDirectory, "Templates", "InvoiceTemplate.html");
        internal string GetLineTemplatePath() => Path.Combine(AppContext.BaseDirectory, "Templates", "InvoiceLineTemplate.html");

        internal IReadOnlyCollection<InvoiceLineDto> BuildInvoiceLines(Order order)
        {
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

            if (startIndex == -1 || endIndex == -1 || endIndex < startIndex) return template;

            var bodyStart = startIndex + startToken.Length;
            return template[..bodyStart] + Environment.NewLine + rows + Environment.NewLine + template[endIndex..];
        }

        internal string BuildLineRow(InvoiceLineDto line)
        {
            var lineTemplate = GetLineTemplate();

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
            var clientAddress = clientDataVersion is null
                ? "n/a"
                : string.IsNullOrWhiteSpace(clientDataVersion.ApartmentNumber)
                    ? $"{clientDataVersion.Street} {clientDataVersion.BuildingNumber}, {clientDataVersion.PostalCode} {clientDataVersion.City}"
                    : $"{clientDataVersion.Street} {clientDataVersion.BuildingNumber}/{clientDataVersion.ApartmentNumber}, {clientDataVersion.PostalCode} {clientDataVersion.City}";

            return template
                .Replace("{{Client.Name}}", clientDataVersion?.ClientName ?? $"Client {clientId}")
                .Replace("{{Client.Address}}", clientAddress)
                .Replace("{{Client.Email}}", clientDataVersion?.AddressEmail ?? "unknown@example.com")
                .Replace("{{Client.Phone}}", clientDataVersion is null ? "n/a" : $"{clientDataVersion.PhonePrefix}{clientDataVersion.PhoneNumber}")
                .Replace("{{Order.ClientId}}", clientId.ToString());
        }

        internal string ApplyStoreTokens(string template)
        {
            const string storeAddress = "Invoice Street 10/2, 00-000 Store";
            return template
                .Replace("{{Store.Name}}", "ECommerce Store")
                .Replace("{{Store.Address}}", storeAddress)
                .Replace("{{Store.Email}}", "support@ecommerce.local")
                .Replace("{{Store.Phone}}", "123123123");
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
            var now = DateTime.UtcNow.ToString("u");
            return template
                .Replace("{{Invoice.Id}}", orderId.ToString())
                .Replace("{{Invoice.IssueDateUtc}}", now)
                .Replace("{{Invoice.GeneratedAtUtc}}", now)
                .Replace("{{#Order.Lines}}", string.Empty)
                .Replace("{{/Order.Lines}}", string.Empty);
        }

        internal string GetInvoicePdfPath(Guid orderId)
        {
            var directory = GetInvoicesDirectoryPath();
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, $"{orderId}.pdf");
        }

        internal string GetInvoicesDirectoryPath() => Path.Combine(ResolveSolutionRoot(), "Invoices");

        internal string ResolveSolutionRoot()
        {
            var fromBase = FindDirectoryContainingSolutionFile(AppContext.BaseDirectory);
            if (fromBase is not null) return fromBase;

            var fromCurrent = FindDirectoryContainingSolutionFile(Directory.GetCurrentDirectory());
            if (fromCurrent is not null) return fromCurrent;

            return Directory.GetCurrentDirectory();
        }

        internal string? FindDirectoryContainingSolutionFile(string startDirectory)
        {
            var directory = new DirectoryInfo(startDirectory);
            while (directory is not null)
            {
                var solutionFile = Path.Combine(directory.FullName, "ECommerceStoreInvoice.slnx");
                if (File.Exists(solutionFile)) return directory.FullName;
                directory = directory.Parent;
            }
            return null;
        }

        internal string GetLineTemplate()
        {
            _cachedLineTemplate ??= File.ReadAllText(GetLineTemplatePath());
            return _cachedLineTemplate;
        }

        internal string FormatMoney(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
        internal string Escape(string value) => WebUtility.HtmlEncode(value);

        public async ValueTask DisposeAsync()
        {
            if (_browser != null) await _browser.DisposeAsync();
            _playwright?.Dispose();
        }
    }
}