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

        public HttpClient CreateClient(WebApplicationFactory<Program> factory)
            // Capture first so the actual response survives an outer contract failure.
            => factory.CreateDefaultClient(new OpenApiResponseHandler(OpenApiDocument), new HttpEvidenceHandler(Evidence));

        private ScenarioHttpEvidence Evidence => _evidence ?? throw new InvalidOperationException("Scenario evidence is not initialized.");

        public JsonSerializerOptions JsonOptions { get; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }
}
