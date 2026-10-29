using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common
{
    public sealed class ScenarioApiContext
    {
        private ScenarioHttpEvidence? _evidence;
        private readonly List<Func<Task>> _additionalCleanup = [];
        internal void RegisterCleanup(Func<Task> cleanup) => _additionalCleanup.Add(cleanup);
        internal async Task CleanupAdditionalAsync()
        {
            var failures = new List<Exception>();
            var callbacks = _additionalCleanup.AsEnumerable().Reverse().ToArray();
            _additionalCleanup.Clear();
            foreach (var cleanup in callbacks)
                try { await cleanup(); }
                catch (Exception exception) { failures.Add(exception); }
            if (failures.Count > 0) throw new AggregateException("Additional scenario cleanup failed.", failures);
        }
        public ApplicationFactory Factory { get; set; } = default!;
        public HttpClient HttpClient { get; set; } = default!;
        public HttpResponseMessage? Response { get; set; }
        public JsonDocument OpenApiDocument { get; set; } = default!;

        public void InitializeEvidence(ScenarioInfo scenario, string feature) => _evidence = ScenarioHttpEvidence.From(scenario, feature);
        public void BeginStep(string phase) => Evidence.StepPhase = phase;
        public void PublishEvidenceIdentity() => Evidence.PublishIdentity();
        public void FlushEvidence(bool final = false) => Evidence.Flush(flushObservations: final);

        public void RecordEvidence(string name, object value) => Evidence.RecordObservation(name, value);

        public HttpClient CreateClient(WebApplicationFactory<Program> factory, string exchangeRole = "api")
            // Capture first so the actual response survives an outer contract failure.
            => factory.CreateDefaultClient(new OpenApiResponseHandler(OpenApiDocument), new HttpEvidenceHandler(Evidence, exchangeRole: exchangeRole));

        // Routing failures have no selected OpenAPI operation. Their complete problems
        // are checked by framework steps and their HTTP bytes use the same recorder.
        internal HttpClient CreateTechnicalClient(WebApplicationFactory<Program> factory) => factory.CreateDefaultClient(new HttpEvidenceHandler(Evidence, exchangeRole: "technical-api"));
        internal DelegatingHandler CreateTechnicalCatalogHandler() => new HttpEvidenceHandler(Evidence, "products", "technical-catalog");

        internal HttpClient CreateRoutingClient() => Factory.CreateDefaultClient(new HttpEvidenceHandler(Evidence));

        internal DelegatingHandler CreateCheckoutCatalogHandler() => new HttpEvidenceHandler(Evidence, "products", "checkout-catalog");

        internal DelegatingHandler CreateOrderCatalogHandler() => new HttpEvidenceHandler(Evidence, "products", "order-catalog");

        internal DelegatingHandler CreateClientVersionCatalogHandler() => new HttpEvidenceHandler(Evidence, "products", "client-version-catalog");

        internal DelegatingHandler CreateInvoiceCatalogHandler() => new HttpEvidenceHandler(Evidence, "products", "invoice-generation-catalog");

        internal DelegatingHandler CreateInvoiceReadCatalogHandler() => new HttpEvidenceHandler(Evidence, "products", "invoice-read-catalog");

        internal DelegatingHandler CreateBusinessCatalogHandler() => new HttpEvidenceHandler(Evidence, "products", "business-validation-catalog");

        internal DelegatingHandler CreateFrameworkCatalogHandler() => new HttpEvidenceHandler(Evidence, "products", "framework-catalog");

        public HttpClient CreateCatalogFixtureClient() => new(new HttpEvidenceHandler(Evidence, "products", "catalog-fixture")
        {
            InnerHandler = new HttpClientHandler()
        })
        {
            BaseAddress = Factory.ProductCatalogBaseAddress,
            Timeout = TimeSpan.FromSeconds(15)
        };

        private ScenarioHttpEvidence Evidence => _evidence ?? throw new InvalidOperationException("Scenario evidence is not initialized.");

        public JsonSerializerOptions JsonOptions { get; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }
}
