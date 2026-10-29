using System.Text.Json;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;

namespace ECommerceStoreInvoice.Acceptance.Tests.Configuration;

public sealed class SourceDocumentationEvidenceTests(ContractDocumentFixture fixture) : IClassFixture<ContractDocumentFixture>
{
    private const string Scenario = "INV14_Unit";
    private const string Instance = "unit-instance";
    private const string CartPath = "/shopping-carts/client/11111111-1111-4111-8111-111111111111";
    [Fact]
    public void CompiledCatalogueHasUniqueQualifiedStepsAndHonestSourceBoundaries()
    {
        var catalogue = SourceDocumentationEvidence.Catalogue.Read();
        Assert.Equal(16, catalogue.Flows.Count); Assert.Equal(128, catalogue.Flows.Values.Sum(flow => flow.StepIds.Length));
        Assert.Equal(128, catalogue.Flows.Values.SelectMany(flow => flow.StepIds).Distinct().Count());
        Assert.Equal(19, catalogue.RuleNames.Count); Assert.Equal(65, catalogue.Outcomes.Count); Assert.Equal(321, catalogue.Cases.Count);
        Assert.Equal(13, fixture.Document.RootElement.GetProperty("paths").EnumerateObject().SelectMany(path => path.Value.EnumerateObject()).Count(operation => catalogue.Flows.ContainsKey(operation.Value.GetProperty("operationId").GetString()!)));
        Assert.Contains("inv.policy.guid-collection", catalogue.Flows["CreateMultipleProductVersions"].PolicyIds);
    }
    [Fact]
    public void ActualWhenWitnessIsPreferredAndSetupRemainsSupporting()
    {
        var result = Build([.. Pair(1, "Given", CartPath, 200), .. Pair(2, "When", CartPath, 200), .. Pair(3, "Then", CartPath, 200)]);
        Assert.Equal(Instance + "/2", result.GetProperty("witnesses")[0].GetProperty("exchangeId").GetString());
        Assert.Equal("setup", result.GetProperty("exchanges")[0].GetProperty("role").GetString());
        Assert.Equal("first", result.GetProperty("exchanges")[1].GetProperty("requestAttempt").GetString());
        Assert.All(result.GetProperty("exchanges").EnumerateArray(), link => { Assert.False(link.GetProperty("executedBranchClaimed").GetBoolean()); Assert.Empty(link.GetProperty("executedStepIds").EnumerateArray()); });
    }
    [Fact]
    public void SetupSuccessCannotSatisfyAnUnobservedScenarioOutcome()
        => Assert.Throws<InvalidOperationException>(() => Build(Pair(1, "Given", CartPath, 200)));
    [Fact]
    public void AnOrphanRawRequestCannotBecomeAValidSourceLink()
        => Assert.Throws<InvalidOperationException>(() => Build(Pair(1, "When", CartPath, 200)[..1]));
    [Fact]
    public void AnObservedErrorFromAnUnrelatedPolicyIsRejected()
        => Assert.Throws<InvalidOperationException>(() => Build(Pair(1, "When", CartPath, 400, "ProductVersionPriceValidationRule"), statuses: [400]));
    [Fact]
    public void CancellationRetainsNominalSourceWithoutInventingAStatusOrExecutedStep()
    {
        var path = "/invoices/11111111-1111-4111-8111-111111111111/pdf";
        var catalogue = Catalogue(path, null, [], "cancelled");
        var request = Pair(1, "When", path, 200)[0];
        var failure = JsonSerializer.SerializeToElement(new { exchangeId = Instance + "/1", scenarioId = Scenario, instanceId = Instance, exceptionType = "OperationCanceledException", cancelled = true });
        var result = JsonSerializer.SerializeToElement(SourceDocumentationEvidence.Build(Scenario, Instance, [request, failure], fixture.Document, catalogue), ScenarioHttpEvidence.JsonOptions);
        Assert.Equal(JsonValueKind.Null, result.GetProperty("witnesses")[0].GetProperty("status").ValueKind);
        Assert.Equal("inv.technical.cancelled-pdf", result.GetProperty("witnesses")[0].GetProperty("outcomeFamilyId").GetString());
        Assert.False(result.GetProperty("executedBranchClaimed").GetBoolean());
    }
    private JsonElement Build(JsonElement[] values, int[]? statuses = null)
        => JsonSerializer.SerializeToElement(SourceDocumentationEvidence.Build(Scenario, Instance, values, fixture.Document, Catalogue(CartPath, "GetShoppingCartByClientId", statuses ?? [200])), ScenarioHttpEvidence.JsonOptions);
    private static SourceDocumentationEvidence.Catalogue Catalogue(string path, string? operation, int[] statuses, string outcome = "response")
    {
        var catalogue = SourceDocumentationEvidence.Catalogue.Read();
        catalogue.Cases.Add(Scenario, new(Scenario, operation, "GET", path, statuses, [], outcome, "Explicit unit scenario intent", [])); return catalogue;
    }
    private static JsonElement[] Pair(int sequence, string phase, string path, int status, string? error = null)
    {
        var request = JsonSerializer.SerializeToElement(new { exchangeId = Instance + "/" + sequence, scenarioId = Scenario, instanceId = Instance, targetService = "invoices", exchangeRole = "api", stepPhase = phase, method = "GET", path, requestAttempt = sequence == 2 ? "first" : null });
        var response = JsonSerializer.SerializeToElement(new { exchangeId = Instance + "/" + sequence, scenarioId = Scenario, instanceId = Instance, status, content = new { body = error is null ? (object)new { } : new { errors = new[] { new { name = error } } } } });
        return [request, response];
    }
}
