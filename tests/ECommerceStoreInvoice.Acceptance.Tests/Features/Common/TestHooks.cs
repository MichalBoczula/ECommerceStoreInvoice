using Allure.Net.Commons;
using Reqnroll;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common
{
    [Binding]
    public sealed class TestHooks
    {
        private readonly ScenarioApiContext _apiContext;
        private readonly ScenarioContext _scenarioContext;
        private ApplicationFactory? _factory;
        private JsonDocument? _openApi;
        private readonly FeatureContext _featureContext;

        public TestHooks(ScenarioApiContext apiContext, ScenarioContext scenarioContext, FeatureContext featureContext)
        {
            _apiContext = apiContext;
            _scenarioContext = scenarioContext;
            _featureContext = featureContext;
        }

        [BeforeScenario]
        public async Task BeforeScenario()
        {
            _apiContext.InitializeEvidence(_scenarioContext.ScenarioInfo, _featureContext.FeatureInfo.Title);
            try
            {
                _factory = new ApplicationFactory(
                    _scenarioContext.ScenarioInfo.Tags.Contains("products-api"),
                    _scenarioContext.ScenarioInfo.Tags.Contains("pdf-fails-once"),
                    _scenarioContext.ScenarioInfo.Tags.Contains("completion-ack-lost"),
                    _scenarioContext.ScenarioInfo.Tags.Contains("pdf-download-fails"),
                    _scenarioContext.ScenarioInfo.Tags.Contains("isolated-products"));
                if (_scenarioContext.ScenarioInfo.Tags.Contains("checkout-evidence"))
                    _factory.CatalogEvidenceHandlerFactory = _apiContext.CreateCheckoutCatalogHandler;
                else if (_scenarioContext.ScenarioInfo.Tags.Contains("order-state-evidence"))
                    _factory.CatalogEvidenceHandlerFactory = _apiContext.CreateOrderCatalogHandler;
                else if (_scenarioContext.ScenarioInfo.Tags.Contains("client-version-evidence"))
                    _factory.CatalogEvidenceHandlerFactory = _apiContext.CreateClientVersionCatalogHandler;
                if (_scenarioContext.ScenarioInfo.Tags.Contains("invoice-generation-evidence"))
                {
                    _factory.CatalogEvidenceHandlerFactory = _apiContext.CreateInvoiceCatalogHandler;
                    _factory.InvoiceProbe = new InvoiceGenerationProbe(_apiContext.RecordEvidence, _factory.GetCheckoutDocumentsAsync,
                        _scenarioContext.ScenarioInfo.Tags.Contains("invoice-hold-pdf"), _scenarioContext.ScenarioInfo.Tags.Contains("invoice-fails-after-write"));
                }
                if (_scenarioContext.ScenarioInfo.Tags.Contains("invoice-read-evidence"))
                {
                    _factory.CatalogEvidenceHandlerFactory = _apiContext.CreateInvoiceReadCatalogHandler;
                    _factory.ReadStorage = new InvoiceReadStorage(_scenarioContext.ScenarioInfo.Tags.Contains("invoice-blob-read"),
                        _scenarioContext.ScenarioInfo.Tags.Contains("invoice-blob-denied"));
                    _factory.ReadProbe = new InvoiceReadProbe(_apiContext.RecordEvidence);
                }
                await _factory.InitializeAsync();
                _apiContext.Factory = _factory;
                using var swaggerClient = _factory.CreateClient();
                _openApi = JsonDocument.Parse(await swaggerClient.GetStringAsync("/swagger/v1/swagger.json"));
                _apiContext.OpenApiDocument = _openApi;
                _apiContext.HttpClient = _apiContext.CreateClient(_factory);
            }
            catch
            {
                await CleanupAsync();
                throw;
            }
        }

        [AfterTestRun]
        public static Task AfterTestRun() => ApplicationFactory.DisposeSharedProductsAsync();

        [BeforeStep]
        public void BeforeStep()
        {
            _apiContext.BeginStep(_scenarioContext.StepContext.StepInfo.StepDefinitionType.ToString());
            _apiContext.PublishEvidenceIdentity();
            var description = _scenarioContext.ScenarioInfo.Description;
            if (string.IsNullOrWhiteSpace(description))
                throw new InvalidOperationException("A readable scenario description is required for BDD evidence.");
            AllureApi.SetDescription(description.Trim());
        }

        [AfterStep]
        public void AfterStep() => _apiContext.FlushEvidence();

        [AfterScenario]
        public async Task AfterScenario()
        {
            try { _apiContext.FlushEvidence(); }
            finally { await CleanupAsync(); }
        }

        private async Task CleanupAsync()
        {
            try
            {
                _apiContext.Response?.Dispose();
                _apiContext.HttpClient?.Dispose();
                _openApi?.Dispose();
            }
            finally
            {
                if (_factory is not null) await _factory.DisposeAsync();
                _factory = null;
                _openApi = null;
            }
        }
    }
}
