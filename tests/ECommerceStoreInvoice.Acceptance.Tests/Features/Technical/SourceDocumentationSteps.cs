using System.Reflection;
using System.Text.Json;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Technical;

[Binding]
public sealed class SourceDocumentationSteps(ScenarioApiContext context)
{
    private Dictionary<string, int> _counts = new();
    private JsonElement _flows;
    private JsonElement _policies;
    [Given("source documentation has this authored inventory")]
    public void GivenInventory(Table table)
    {
        _counts = table.Rows.ToDictionary(row => row["Kind"], row => int.Parse(row["Count"], System.Globalization.CultureInfo.InvariantCulture));
        context.RecordEvidence("Source documentation authored inventory table", _counts);
    }
    [When("I read the authored flow and policy descriptions")]
    public async Task WhenRead()
    {
        using var flows = await context.HttpClient.GetAsync("/orders-documentation/flows");
        using var policies = await context.HttpClient.GetAsync("/orders-documentation/validations");
        flows.EnsureSuccessStatusCode(); policies.EnsureSuccessStatusCode();
        using var f = JsonDocument.Parse(await flows.Content.ReadAsByteArrayAsync());
        using var p = JsonDocument.Parse(await policies.Content.ReadAsByteArrayAsync());
        _flows = f.RootElement.Clone(); _policies = p.RootElement.Clone();
    }
    [Then("every source identity and description is coherent without inventing HTTP reachability")]
    public void ThenSource()
    {
        var catalogue = SourceDocumentationEvidence.Catalogue.Read();
        catalogue.Flows.Count.ShouldBe(_counts["Flows"]);
        catalogue.Flows.Values.Sum(flow => flow.StepIds.Length).ShouldBe(_counts["Steps"]);
        catalogue.RuleNames.Count.ShouldBe(_counts["PolicyRules"] + _counts["InlineRules"]);
        catalogue.Outcomes.Count.ShouldBe(_counts["OutcomeFamilies"]);
        _flows.GetProperty("flows").GetArrayLength().ShouldBe(_counts["PublishedFlows"]);
        _policies.GetProperty("validations").GetArrayLength().ShouldBe(_counts["Policies"]);
        var sourceTypes = typeof(FlowDescriptor).Assembly.GetTypes().Where(type => type.BaseType?.IsGenericType == true && type.BaseType.GetGenericTypeDefinition().Name == "FlowDescriberBase`1").ToDictionary(type => type.BaseType!.GenericTypeArguments.Single().Name);
        foreach (var wrapper in _flows.GetProperty("flows").EnumerateArray())
            foreach (var flow in wrapper.EnumerateObject())
            {
                var name = flow.Value.GetProperty("flowName").GetString()!;
                var type = sourceTypes[name]; var identity = type.GetCustomAttribute<DocumentationAttribute>()!;
                identity.Id.ShouldBe(catalogue.Flows[name].Id); identity.Summary.ShouldNotBeNullOrWhiteSpace();
                var steps = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Select(method => method.GetCustomAttribute<FlowStepAttribute>()).Where(step => step is not null).OrderBy(step => step!.Order).ToArray();
                var actual = flow.Value.GetProperty("steps").EnumerateArray().ToArray(); actual.Length.ShouldBe(steps.Length);
                foreach (var (step, index) in steps.Select((step, index) => (step!, index)))
                {
                    actual[index].GetProperty("order").GetInt32().ShouldBe(step.Order);
                    actual[index].GetProperty("bpmnId").GetString().ShouldBe(step.BpmnId);
                    actual[index].GetProperty("stepName").GetString().ShouldBe(step.Description);
                    step.Description.ShouldNotBeNullOrWhiteSpace();
                }
            }
        var named = context.OpenApiDocument.RootElement.GetProperty("paths").EnumerateObject().SelectMany(path => path.Value.EnumerateObject()).Select(operation => operation.Value.GetProperty("operationId").GetString()!).ToArray();
        named.Count(catalogue.Flows.ContainsKey).ShouldBe(_counts["HttpFlows"]);
        named.ShouldNotContain("CreateMultipleProductVersions"); named.ShouldNotContain("CreateProductVersion"); named.ShouldNotContain("GetProductVersionById");
        context.RecordEvidence("Source documentation complete identity and prose verified", new
        {
            counts = _counts,
            actualFlows = _flows,
            actualPolicies = _policies,
            compiledFlows = catalogue.Flows.Values.OrderBy(flow => flow.Id).ToArray(),
            outcomeFamilies = catalogue.Outcomes.Values.OrderBy(outcome => outcome.Id).Select(outcome => new { outcome.Id, outcome.OperationId, status = outcome.Status == 0 ? (int?)null : outcome.Status, outcome.Condition, outcome.Effects, outcome.Failure }).ToArray(),
            internalServiceUnpublished = "CreateMultipleProductVersions",
            publishedWithoutDirectHttp = new[] { "CreateProductVersion", "GetProductVersionById" },
            declaredMetadataIsNotExecutionTracing = true
        });
    }
}
