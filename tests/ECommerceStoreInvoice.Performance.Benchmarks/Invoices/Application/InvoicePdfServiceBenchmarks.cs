using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Application.Services.Concrete.Invoices;
using ECommerceStoreInvoice.Domain.AggregatesModel.OrderAggregate;
using ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace ECommerceStoreInvoice.Performance.Benchmarks.Invoices.Application
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class InvoicePdfServiceBenchmarks
    {
        private IServiceProvider _serviceProvider = null!;
        private IInvoicePdfService _service = null!;
        private Order _order = null!;
        private ClientDataVersionResponseDto _client = null!;
        private static bool _playwrightInstalled;
        private static readonly SemaphoreSlim PlaywrightInstallSemaphore = new(1, 1);

        [Params(1, 10)] // 100 może zająć wieczność przy uruchamianiu browsera per test
        public int LinesCount { get; set; }

        [GlobalSetup]
        public async Task Setup()
        {
            await EnsurePlaywrightInstalledAsync();
            SetupTemplates();

            var services = new ServiceCollection();
            services.AddScoped<IInvoicePdfService, InvoicePdfService>();
            _serviceProvider = services.BuildServiceProvider();
            _service = _serviceProvider.GetRequiredService<IInvoicePdfService>();

            _order = InvoicePdfServiceBenchmarkDataFactory.CreateOrder(LinesCount);
            _client = InvoicePdfServiceBenchmarkDataFactory.CreateClient();
        }

        [Benchmark]
        public async Task GenerateInvoicePdf_FullFlow()
        {
            await _service.GenerateInvoicePdf(_order, _client);
        }

        private void SetupTemplates()
        {
            var templateDir = Path.Combine(AppContext.BaseDirectory, "Templates");
            Directory.CreateDirectory(templateDir);

            var mainTemplate = @"<html><body><h1>Invoice {{InvoiceNumber}}</h1><tbody></tbody></body></html>";
            var lineTemplate = @"<tr><td>{{Line.Name}}</td><td>{{Line.TotalAmount}}</td></tr>";

            File.WriteAllText(Path.Combine(templateDir, "InvoiceTemplate.html"), mainTemplate);
            File.WriteAllText(Path.Combine(templateDir, "InvoiceLineTemplate.html"), lineTemplate);
        }

        private static async Task EnsurePlaywrightInstalledAsync()
        {
            if (_playwrightInstalled) return;
            await PlaywrightInstallSemaphore.WaitAsync();
            try
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
                using var process = Process.Start(startInfo) ?? throw new Exception("Failed to start PW install");
                await process.WaitForExitAsync();
                _playwrightInstalled = true;
            }
            finally { PlaywrightInstallSemaphore.Release(); }
        }

        private static string FindPlaywrightScriptPath()
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
    }
}
