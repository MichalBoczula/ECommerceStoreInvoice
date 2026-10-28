using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common
{
    public sealed class ScenarioApiContext
    {
        private ScenarioHttpEvidence? _evidence;
        public ApplicationFactory Factory { get; set; } = default!;
        public HttpClient HttpClient { get; set; } = default!;
        public HttpResponseMessage? Response { get; set; }
        public JsonDocument OpenApiDocument { get; set; } = default!;

        public void InitializeEvidence(ScenarioInfo scenario, string feature) => _evidence = ScenarioHttpEvidence.From(scenario, feature);
        public void BeginStep(string phase) => Evidence.StepPhase = phase;
        public void PublishEvidenceIdentity() => Evidence.PublishIdentity();
        public void FlushEvidence() => Evidence.Flush();

        public void RecordEvidence(string name, object value) => Evidence.Record(name, new
        {
            service = "invoices",
            scenarioId = Evidence.ScenarioId,
            instanceId = Evidence.InstanceId,
            value
        });

        public HttpClient CreateClient(WebApplicationFactory<Program> factory)
            // Capture first so the actual response survives an outer contract failure.
            => factory.CreateDefaultClient(new OpenApiResponseHandler(OpenApiDocument), new HttpEvidenceHandler(Evidence));

        internal DelegatingHandler CreateCheckoutCatalogHandler() => new HttpEvidenceHandler(Evidence, "products", "checkout-catalog");

        internal DelegatingHandler CreateOrderCatalogHandler() => new HttpEvidenceHandler(Evidence, "products", "order-catalog");

        internal DelegatingHandler CreateClientVersionCatalogHandler() => new HttpEvidenceHandler(Evidence, "products", "client-version-catalog");

        internal DelegatingHandler CreateInvoiceCatalogHandler() => new HttpEvidenceHandler(Evidence, "products", "invoice-generation-catalog");

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
