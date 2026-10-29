using System.Globalization;
using System.Reflection;
using System.Text.Json;
using ECommerceStoreInvoice.API.Configuration;
using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Domain;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic.FileIO;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common;

/// <summary>Joins declared source IDs to immutable actual HTTP records; it never reports executed steps.</summary>
internal static class SourceDocumentationEvidence
{
    internal sealed record Flow(string Id, string Name, string[] StepIds, string[] PolicyIds, string[] RuleIds);
    internal sealed record Binding(string ScenarioId, string? OperationId, string Method, string Path, int[] Statuses, int[] OptionalStatuses, string Outcome, string Cause, string[] ExpectedRules);
    internal sealed record Witness(int? Status, string ExchangeId, string OutcomeFamilyId);
    private static readonly Lazy<Catalogue> Source = new(Catalogue.Read);

    internal static object Build(string scenarioId, string instanceId, JsonElement[] records, JsonDocument document, bool verify = true)
        => Build(scenarioId, instanceId, records, document, Source.Value, verify);

    internal static object Build(string scenarioId, string instanceId, JsonElement[] records, JsonDocument document, Catalogue catalogue, bool verify = true)
    {
        if (!catalogue.Cases.TryGetValue(scenarioId, out var binding)) throw new InvalidOperationException("Missing source scenario binding: " + scenarioId);
        var links = new List<object>();
        var candidates = new List<(int Sequence, string Phase, int? Status, string ExchangeId, string Family, string[] Rules)>();
        foreach (var group in records.GroupBy(record => record.GetProperty("exchangeId").GetString()!))
        {
            var pair = group.ToArray();
            if (pair.Length != 2) { if (verify) throw new InvalidOperationException("Unpaired actual HTTP source link: " + group.Key); else continue; }
            var request = pair.Single(value => !value.TryGetProperty("status", out _) && !value.TryGetProperty("exceptionType", out _));
            var outcome = pair.Single(value => value.TryGetProperty("status", out _) || value.TryGetProperty("exceptionType", out _));
            foreach (var value in pair)
                if (value.GetProperty("scenarioId").GetString() != scenarioId || value.GetProperty("instanceId").GetString() != instanceId)
                    throw new InvalidOperationException("Foreign actual HTTP identity in source evidence.");
            var target = request.GetProperty("targetService").GetString();
            var role = request.GetProperty("exchangeRole").GetString()!;
            var phase = request.GetProperty("stepPhase").GetString()!;
            var method = request.GetProperty("method").GetString()!;
            var path = request.GetProperty("path").GetString()!;
            var sequence = int.Parse(group.Key[(group.Key.LastIndexOf('/') + 1)..], CultureInfo.InvariantCulture);
            var operation = target == "invoices" ? ResolveOperation(document, method, path) : null;
            catalogue.Flows.TryGetValue(operation ?? string.Empty, out var flow);
            var status = outcome.TryGetProperty("status", out var code) ? (int?)code.GetInt32() : null;
            var family = target != "invoices" ? null : status is null ? "inv.technical.cancelled-pdf"
                : operation is not null ? "inv.outcome." + Slug(operation) + "." + status
                : path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ? "inv.technical.health." + status
                : path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ? "inv.technical.swagger." + status
                : "inv.framework.routing." + status;
            if (verify && family is not null && (!catalogue.Outcomes.TryGetValue(family, out var declared) || declared.Status != (status ?? 0)))
                throw new InvalidOperationException("Undeclared actual HTTP outcome family: " + family);
            var observed = new HashSet<string>(StringComparer.Ordinal);
            if (target == "invoices" && outcome.TryGetProperty("content", out var content) && content.TryGetProperty("body", out var body) && body.ValueKind == JsonValueKind.Object && body.TryGetProperty("errors", out var errors))
                foreach (var error in errors.EnumerateArray())
                {
                    var name = error.GetProperty("name").GetString()!;
                    if (!catalogue.RuleNames.TryGetValue(name, out var id)) throw new InvalidOperationException("Undocumented observed error rule: " + name);
                    observed.Add(id);
                }
            var related = observed.Count == 0 || flow is not null && !observed.Except(flow.RuleIds).Any();
            if (verify && !related) throw new InvalidOperationException("Actual error rule is unrelated to the nominal source flow.");
            var candidate = target == "invoices" && role != "api-peer" && phase is "When" or "Then" &&
                (binding.OperationId is not null ? operation == binding.OperationId : method == binding.Method && Matches(binding.Path, path)) &&
                (binding.Outcome == "cancelled" ? status is null : status is not null && binding.Statuses.Concat(binding.OptionalStatuses).Contains(status.Value));
            if (candidate) candidates.Add((sequence, phase, status, group.Key, family!, observed.Order().ToArray()));
            links.Add(new
            {
                exchangeId = group.Key,
                sequence,
                targetService = target,
                exchangeRole = role,
                stepPhase = phase,
                method,
                path,
                operationId = operation,
                query = request.TryGetProperty("query", out var query) ? query.GetString() : null,
                requestAttempt = request.TryGetProperty("requestAttempt", out var attempt) ? attempt.GetString() : null,
                legacyAlias = operation == "CreateOrder" && path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length == 2,
                status,
                cancelled = status is null && outcome.GetProperty("cancelled").GetBoolean(),
                requestAttachmentName = $"HTTP {sequence:000} request",
                outcomeAttachmentName = $"HTTP {sequence:000} " + (status is null ? "failure" : "response"),
                role = target != "invoices" ? "external-dependency" : candidate ? "scenario-outcome-candidate" : phase is "Given" or "Setup" ? "setup" : "supporting",
                nominalFlowId = flow?.Id,
                declaredStepIds = flow?.StepIds ?? [],
                declaredPolicyIds = flow?.PolicyIds ?? [],
                declaredRuleIds = flow?.RuleIds ?? [],
                outcomeFamilyId = family,
                observedErrorRuleIds = observed.Order().ToArray(),
                observedRulesDeclaredByNominalFlow = related,
                executedStepIds = Array.Empty<string>(),
                executedBranchClaimed = false
            });
        }
        int?[] required = binding.Outcome == "cancelled" ? [null] : binding.Statuses.Select(status => (int?)status).ToArray();
        var witnesses = new List<Witness>();
        foreach (var status in required)
        {
            var selected = candidates.Where(value => value.Status == status).OrderBy(value => value.Phase == "When" ? 0 : 1).ThenBy(value => value.Sequence).ToArray();
            if (selected.Length == 0) { if (verify) throw new InvalidOperationException($"No actual When/Then witness for {scenarioId} {status?.ToString(CultureInfo.InvariantCulture) ?? "cancelled"}."); else continue; }
            witnesses.Add(new(status, selected[0].ExchangeId, selected[0].Family));
        }
        if (verify && binding.ExpectedRules.Except(candidates.SelectMany(candidate => candidate.Rules)).Any()) throw new InvalidOperationException("Authored scenario rule expectation has no observed response error.");
        return new
        {
            schemaVersion = 1,
            service = "invoices",
            scenarioId,
            instanceId,
            basis = "Declared source metadata and authored scenario intent joined to actual HTTP. Nominal steps/policies are not execution telemetry; response errors alone identify observed rules.",
            binding = new { binding.OperationId, binding.Method, binding.Path, guaranteedStatuses = binding.Statuses, optionalStatuses = binding.OptionalStatuses, binding.Outcome, binding.Cause, expectedRuleIds = binding.ExpectedRules },
            witnesses,
            exchanges = links,
            everyGuaranteedOutcomeWitnessed = witnesses.Count == required.Length,
            executedBranchClaimed = false
        };
    }

    internal static string? ResolveOperation(JsonDocument document, string method, string path)
    {
        if (method == "POST" && path.StartsWith("/orders/", StringComparison.OrdinalIgnoreCase) && path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length == 2)
            path = "/orders/client/" + path.Split('/').Last();
        foreach (var route in document.RootElement.GetProperty("paths").EnumerateObject())
            if (Matches(route.Name, path) && route.Value.TryGetProperty(method.ToLowerInvariant(), out var operation)) return operation.GetProperty("operationId").GetString();
        return null;
    }
    private static bool Matches(string template, string path)
    {
        var expected = template.Split('/', StringSplitOptions.RemoveEmptyEntries); var actual = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return expected.Length == actual.Length && expected.Zip(actual).All(value => value.First.StartsWith('{') ? Guid.TryParse(value.Second, out _) : string.Equals(value.First, value.Second, StringComparison.OrdinalIgnoreCase));
    }
    private static string Slug(string value) => System.Text.RegularExpressions.Regex.Replace(value, "(?<!^)(?=[A-Z])", "-").ToLowerInvariant();

    internal sealed class Catalogue
    {
        internal Dictionary<string, Flow> Flows { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, string> RuleNames { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, HttpOutcomeAttribute> Outcomes { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, Binding> Cases { get; } = new(StringComparer.Ordinal);
        internal static Catalogue Read()
        {
            var result = new Catalogue(); var registrations = new ServiceCollection().AddDomain();
            using var provider = registrations.BuildServiceProvider(); using var scope = provider.CreateScope();
            var policyTypes = registrations.Where(service => service.ServiceType.IsGenericType && service.ServiceType.GetGenericTypeDefinition() == typeof(IValidationPolicy<>)).ToDictionary(service => service.ServiceType, service => service.ImplementationType!);
            var ruleTypes = typeof(DocumentationAttribute).Assembly.GetTypes().Where(type => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidationRule<>))).ToArray();
            foreach (var rule in ruleTypes) result.RuleNames.Add(rule.Name, Metadata(rule).Id);
            var policies = scope.ServiceProvider.GetServices<IValidationPolicyDescriptorProvider>().ToDictionary(value => value.GetType().Name, value => new { Id = Metadata(value.GetType()).Id, Rules = value.Describe().Rules.Select(rule => result.RuleNames[rule.RuleName]).ToArray() });
            foreach (var type in typeof(FlowDescriptor).Assembly.GetTypes().Where(type => type.BaseType?.IsGenericType == true && type.BaseType.GetGenericTypeDefinition().Name == "FlowDescriberBase`1"))
            {
                var identity = Metadata(type).Id; var name = type.BaseType!.GenericTypeArguments.Single().Name;
                var steps = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Select(method => (Method: method, Step: method.GetCustomAttribute<FlowStepAttribute>())).Where(item => item.Step is not null).OrderBy(item => item.Step!.Order).ToArray();
                var declared = steps.SelectMany(item => item.Method.GetParameters()).Where(parameter => policyTypes.ContainsKey(parameter.ParameterType)).Select(parameter => policyTypes[parameter.ParameterType].Name).Concat(type.GetCustomAttributes<UsesPolicyAttribute>().Select(attribute => attribute.Name)).Distinct().Select(policy => policies[policy]).ToArray();
                var inline = steps.Select(item => item.Method.GetCustomAttribute<InlineRuleAttribute>()).Where(item => item is not null).Select(item => item!).ToArray();
                foreach (var rule in inline) result.RuleNames.Add(rule.ErrorName, rule.Id);
                result.Flows.Add(name, new(identity, name, steps.Select(item => identity + "." + item.Step!.BpmnId).ToArray(), declared.Select(policy => policy.Id).Order().ToArray(), declared.SelectMany(policy => policy.Rules).Concat(inline.Select(rule => rule.Id)).Distinct().Order().ToArray()));
            }
            foreach (var outcome in typeof(InvoiceHttpDocumentation).GetCustomAttributes<HttpOutcomeAttribute>()) result.Outcomes.Add(outcome.Id, outcome);
            foreach (var resource in new[] { "Invoices.Bdd.Matrix.tsv", "Invoices.Bdd.Framework.tsv", "Invoices.Bdd.Technical.tsv" })
            {
                using var stream = typeof(SourceDocumentationEvidence).Assembly.GetManifestResourceStream(resource)!;
                using var parser = new TextFieldParser(stream); parser.SetDelimiters("\t"); parser.HasFieldsEnclosedInQuotes = true;
                var headers = parser.ReadFields()!;
                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields()!; var row = headers.Zip(fields).ToDictionary(pair => pair.First, pair => pair.Second);
                    var rules = Array.Empty<string>();
                    if (row["example"] != "null")
                    {
                        using var example = JsonDocument.Parse(row["example"]);
                        if (example.RootElement.TryGetProperty("Rule", out var rule) && result.RuleNames.TryGetValue(rule.GetString()!, out var id)) rules = [id];
                    }
                    result.Cases.Add(row["scenarioId"], new(row["scenarioId"], row.GetValueOrDefault("operationId"), row["method"], row["path"], Statuses(row["statuses"]), Statuses(row["optionalStatuses"]), row.GetValueOrDefault("outcome", "response"), row["cause"], rules));
                }
            }
            return result;
        }
        private static int[] Statuses(string value) => value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray();
        private static DocumentationAttribute Metadata(Type type) => type.GetCustomAttribute<DocumentationAttribute>() ?? throw new InvalidOperationException("Missing authored source identity: " + type.Name);
    }
}
