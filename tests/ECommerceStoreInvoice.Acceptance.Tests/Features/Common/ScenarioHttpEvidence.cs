using Allure.Net.Commons;
using Reqnroll;
using System.Collections;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common;

/// <summary>Scenario-local evidence, including HTTP calls made by fault clients and server callbacks.</summary>
internal sealed class ScenarioHttpEvidence
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly ConcurrentQueue<(string Name, object? Value, byte[]? Pdf)> _pending = new();
    private int _sequence;
    private bool _publishedIdentity;
    private readonly bool _labelFromTag;

    internal ScenarioHttpEvidence(string scenarioId, string feature, string scenario, SortedDictionary<string, string?> parameters, bool labelFromTag = false)
    {
        if (!Regex.IsMatch(scenarioId, "^INV[0-9]{2}_[A-Za-z0-9_]+$"))
            throw new InvalidOperationException("A stable scenarioId is required for HTTP evidence.");
        ScenarioId = scenarioId;
        Feature = feature;
        Scenario = scenario;
        Parameters = new SortedDictionary<string, string?>(parameters, StringComparer.Ordinal);
        _labelFromTag = labelFromTag;
        InstanceId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { scenarioId, parameters = Parameters })))).ToLowerInvariant();
    }

    internal string ScenarioId { get; }
    internal string Feature { get; }
    internal string Scenario { get; }
    internal SortedDictionary<string, string?> Parameters { get; }
    internal string InstanceId { get; }
    internal string StepPhase { get; set; } = "Setup";

    internal static ScenarioHttpEvidence From(ScenarioInfo info, string feature)
    {
        const string prefix = "allure.label.scenarioId:";
        var tag = info.Tags.SingleOrDefault(value => value.StartsWith(prefix, StringComparison.Ordinal));
        var argument = info.Arguments?["scenarioId"]?.ToString();
        if ((tag is null) == (argument is null))
            throw new InvalidOperationException("Specify scenarioId once: a scenario tag or an Examples column.");
        var parameters = new SortedDictionary<string, string?>(StringComparer.Ordinal);
        if (info.Arguments is not null)
            foreach (DictionaryEntry entry in info.Arguments)
                if ((string)entry.Key != "scenarioId")
                    parameters.Add((string)entry.Key, entry.Value?.ToString());
        return new ScenarioHttpEvidence(argument ?? tag![prefix.Length..], feature, info.Title, parameters, tag is not null);
    }

    internal int NextExchange() => Interlocked.Increment(ref _sequence);

    internal void Record(string name, object value) => _pending.Enqueue((name, value, null));

    internal void RecordPdf(string name, byte[] bytes) => _pending.Enqueue((name, null, bytes.ToArray()));

    internal object Identity() => new { service = "invoices", scenarioId = ScenarioId, instanceId = InstanceId, feature = Feature, scenario = Scenario, parameters = Parameters };

    internal void PublishIdentity()
    {
        if (_publishedIdentity)
            return;
        _publishedIdentity = true;
        // Plain scenarios already receive this label from their Gherkin tag.
        if (!_labelFromTag)
            AllureApi.AddLabel("scenarioId", ScenarioId);
        AllureApi.AddLabel("service", "invoices");
        AllureApi.AddLabel("instanceId", InstanceId);
        Attach("Scenario identity", Identity());
    }

    internal void Flush(Action<string, object>? attach = null, Action<string, byte[]>? attachPdf = null)
    {
        while (_pending.TryDequeue(out var item))
        {
            if (item.Pdf is not null)
                (attachPdf ?? AttachPdf)(item.Name, item.Pdf);
            else
                (attach ?? Attach)(item.Name, item.Value!);
        }
    }

    private static void AttachPdf(string name, byte[] bytes) => AllureApi.AddAttachment(name, "application/pdf", bytes, ".pdf");

    private static void Attach(string name, object value) => AllureApi.AddAttachment(name, "application/json",
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonOptions)), ".json");
}

