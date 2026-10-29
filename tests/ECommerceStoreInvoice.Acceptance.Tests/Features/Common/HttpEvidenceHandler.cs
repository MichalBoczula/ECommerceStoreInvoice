using System.Security.Cryptography;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common;

/// <summary>Captures actual client-visible bytes inside contract validation without replacing content.</summary>
internal sealed class HttpEvidenceHandler(ScenarioHttpEvidence evidence, string targetService = "invoices", string exchangeRole = "api") : DelegatingHandler
{
    internal static readonly HttpRequestOptionsKey<TaskCompletionSource> CompletionKey = new("Invoices.HttpEvidence.Completion");
    internal static readonly HttpRequestOptionsKey<TaskCompletionSource> ResponseHeadersKey = new("Invoices.HttpEvidence.ResponseHeaders");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sequence = evidence.NextExchange();
        var exchangeId = $"{evidence.InstanceId}/{sequence}";
        var phase = evidence.StepPhase;
        var method = request.Method.Method;
        var path = request.RequestUri?.AbsolutePath;
        var query = request.RequestUri?.Query;
        var requestAttempt = request.Headers.TryGetValues("X-Bdd-Attempt", out var attempts) ? attempts.Single() : null;
        var stage = "request-body";
        var requestRecorded = false;
        HttpResponseMessage? response = null;
        void RecordRequest(ContentEvidence content)
        {
            evidence.Record($"HTTP {sequence:000} request", new
            {
                service = "invoices",
                targetService,
                exchangeRole,
                scenarioId = evidence.ScenarioId,
                instanceId = evidence.InstanceId,
                exchangeId,
                requestAttempt,
                stepPhase = phase,
                method,
                path,
                query,
                content
            });
            requestRecorded = true;
        }
        try
        {
            RecordRequest(await ReadContent(request.Content, cancellationToken));
            stage = "transport";
            response = await base.SendAsync(request, cancellationToken);
            stage = "response-body";
            if (request.Options.TryGetValue(ResponseHeadersKey, out var headers))
                headers.TrySetResult();
            var content = await ReadContent(response.Content, cancellationToken);
            evidence.Record($"HTTP {sequence:000} response", new
            {
                service = "invoices",
                targetService,
                exchangeRole,
                scenarioId = evidence.ScenarioId,
                instanceId = evidence.InstanceId,
                exchangeId,
                requestAttempt,
                stepPhase = phase,
                method,
                path,
                query,
                status = (int)response.StatusCode,
                location = response.Headers.Location?.ToString(),
                contentDisposition = response.Content.Headers.ContentDisposition?.ToString(),
                cacheControl = response.Headers.CacheControl?.ToString(),
                allow = response.Content.Headers.Allow.ToArray(),
                content
            });
            if (response.Content.Headers.ContentType?.MediaType?.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) == true)
                evidence.RecordPdf($"HTTP {sequence:000} response PDF ({exchangeId})", Convert.FromBase64String(content.BodyBytesBase64!));
            return response;
        }
        catch (Exception error)
        {
            response?.Dispose();
            if (!requestRecorded)
                RecordRequest(new ContentEvidence(request.Content?.Headers.ContentType?.ToString(), "unavailable", null, null, null, null, null, error.GetType().Name));
            evidence.Record($"HTTP {sequence:000} failure", new
            {
                service = "invoices",
                targetService,
                exchangeRole,
                scenarioId = evidence.ScenarioId,
                instanceId = evidence.InstanceId,
                exchangeId,
                requestAttempt,
                stepPhase = phase,
                method,
                path,
                query,
                failureStage = stage,
                exceptionType = error.GetType().Name,
                cancelled = error is OperationCanceledException || cancellationToken.IsCancellationRequested,
                cancellationRequested = cancellationToken.IsCancellationRequested
            });
            throw;
        }
        finally
        {
            if (request.Options.TryGetValue(CompletionKey, out var completion))
                completion.TrySetResult();
        }
    }

    private static async Task<ContentEvidence> ReadContent(HttpContent? content, CancellationToken cancellationToken)
    {
        if (content is null)
            return new ContentEvidence(null, "none", null, null, 0, null, null);
        var bytes = await content.ReadAsByteArrayAsync(cancellationToken);
        var media = content.Headers.ContentType?.MediaType;
        var json = media?.EndsWith("/json", StringComparison.OrdinalIgnoreCase) == true ||
                   media?.EndsWith("+json", StringComparison.OrdinalIgnoreCase) == true;
        var text = json || media?.StartsWith("text/", StringComparison.OrdinalIgnoreCase) == true;
        var raw = text ? await content.ReadAsStringAsync(cancellationToken) : null;
        object? body = raw;
        var format = bytes.Length == 0 ? "empty" : text ? "text" : "binary";
        if (bytes.Length == 0)
            body = null;
        else if (json)
        {
            try
            {
                using var document = JsonDocument.Parse(raw!);
                body = document.RootElement.Clone();
                format = "json";
            }
            catch (JsonException) { format = "invalid-json"; }
        }
        return new ContentEvidence(content.Headers.ContentType?.ToString(), format, body, raw, bytes.Length,
            Convert.ToBase64String(bytes), Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
    }

    private sealed record ContentEvidence(string? ContentType, string BodyFormat, object? Body, string? RawBody,
        int? ByteLength, string? BodyBytesBase64, string? Sha256, string? CaptureExceptionType = null);
}
