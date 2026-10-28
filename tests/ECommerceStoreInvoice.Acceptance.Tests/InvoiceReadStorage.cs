using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace ECommerceStoreInvoice.Acceptance.Tests;

internal sealed record InvoiceReadArtifact(string Key, string StorageUrl, bool Exists, long Length, string? Sha256, string? ContentType, DateTime? LastModified, string? ETag);

/// <summary>Owned local files or private Azurite objects; SDK fixture operations are separate from API HTTP evidence.</summary>
internal sealed class InvoiceReadStorage(bool blob, bool denied) : IAsyncDisposable
{
    private readonly string _key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly string _wrongKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly Dictionary<string, (string Url, byte[] Bytes)> _artifacts = new(StringComparer.Ordinal);
    private IContainer? _azurite;
    private BlobContainerClient? _container;
    internal bool IsBlob => blob;
    internal bool AccessDenied => denied;
    internal string ContainerName { get; } = $"invoice-read-{Guid.NewGuid():N}";
    private string _connection = string.Empty;
    internal string ApplicationConnectionString => denied ? _connection.Replace(_key, _wrongKey, StringComparison.Ordinal) : _connection;

    internal async Task InitializeAsync()
    {
        if (!blob) return;
        _azurite = new ContainerBuilder("mcr.microsoft.com/azure-storage/azurite:3.35.0")
            .WithEnvironment("AZURITE_ACCOUNTS", $"invoicetest:{_key}")
            .WithCommand("azurite-blob", "--blobHost", "0.0.0.0", "--skipApiVersionCheck")
            .WithPortBinding(10000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Azurite Blob service successfully listens on",
                options => options.WithTimeout(TimeSpan.FromSeconds(60))))
            .Build();
        await _azurite.StartAsync();
        _connection = $"DefaultEndpointsProtocol=http;AccountName=invoicetest;AccountKey={_key};BlobEndpoint=http://{_azurite.Hostname}:{_azurite.GetMappedPublicPort(10000)}/invoicetest;";
        _container = new BlobContainerClient(_connection, ContainerName);
        await _container.CreateAsync(PublicAccessType.None);
        if ((await _container.GetAccessPolicyAsync()).Value.BlobPublicAccess != PublicAccessType.None)
            throw new InvalidOperationException("Invoice read fixture container must remain private.");
    }

    internal async Task<string> PutAsync(string key, Guid invoiceId, Guid attemptId, byte[] bytes, bool missing)
    {
        var name = $"{invoiceId:N}-{attemptId:N}.pdf";
        string url;
        if (blob)
        {
            var client = _container!.GetBlobClient(name);
            url = client.Uri.AbsoluteUri;
            if (!missing)
                await client.UploadAsync(new MemoryStream(bytes), new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "application/pdf" } });
        }
        else
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root.Parent is not null && !File.Exists(Path.Combine(root.FullName, "ECommerceStoreInvoice.slnx"))) root = root.Parent;
            var directory = Path.Combine(root.FullName, "Invoices");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, name);
            url = new Uri(path).AbsoluteUri;
            if (!missing) await File.WriteAllBytesAsync(path, bytes);
        }
        _artifacts.Add(key, (url, bytes.ToArray()));
        return url;
    }

    internal void TrackGeneratedFile(string key, string url)
    {
        var uri = new Uri(url);
        if (!uri.IsFile) throw new InvalidOperationException("The generated local-read fixture must use a real local PDF.");
        _artifacts.Add(key, (url, File.ReadAllBytes(uri.LocalPath)));
    }
    internal byte[] ExpectedBytes(string key) => _artifacts[key].Bytes.ToArray();
    internal object ExpectedEvidence() => _artifacts.ToDictionary(pair => pair.Key, pair => new
    {
        storageUrl = pair.Value.Url,
        byteLength = pair.Value.Bytes.Length,
        sha256 = Hash(pair.Value.Bytes),
        bodyBytesBase64 = Convert.ToBase64String(pair.Value.Bytes)
    });
    internal async Task<InvoiceReadArtifact[]> ReadStateAsync()
    {
        var result = new List<InvoiceReadArtifact>();
        foreach (var pair in _artifacts.OrderBy(pair => pair.Key))
        {
            var url = pair.Value.Url;
            byte[]? bytes = null;
            string? contentType = null;
            DateTime? modified = null;
            string? etag = null;
            if (blob)
            {
                var client = _container!.GetBlobClient(new Uri(url).Segments.Last());
                if ((await client.ExistsAsync()).Value)
                {
                    bytes = (await client.DownloadContentAsync()).Value.Content.ToArray();
                    var properties = (await client.GetPropertiesAsync()).Value;
                    contentType = properties.ContentType;
                    modified = properties.LastModified.UtcDateTime;
                    etag = properties.ETag.ToString();
                }
            }
            else if (File.Exists(new Uri(url).LocalPath))
            {
                bytes = await File.ReadAllBytesAsync(new Uri(url).LocalPath);
                modified = File.GetLastWriteTimeUtc(new Uri(url).LocalPath);
            }
            result.Add(new(pair.Key, url, bytes is not null, bytes?.LongLength ?? 0, bytes is null ? null : Hash(bytes), contentType, modified, etag));
        }
        return result.ToArray();
    }
    internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    internal static byte[] BinaryPdf(string label)
    {
        using var stream = new MemoryStream();
        void Write(string text) => stream.Write(Encoding.ASCII.GetBytes(text));
        Write("%PDF-1.7\n%");
        stream.Write(new byte[] { 0xe2, 0xe3, 0xcf, 0xd3, 10 });
        var content = $"BT /F1 12 Tf 72 720 Td ({label}) Tj ET\n";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream"
        };
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(stream.Position);
            Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }
        var xref = stream.Position;
        Write("xref\n0 6\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write($"{offset.ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");
        Write($"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return stream.ToArray();
    }
    public async ValueTask DisposeAsync()
    {
        foreach (var artifact in _artifacts.Values)
            if (new Uri(artifact.Url).IsFile) File.Delete(new Uri(artifact.Url).LocalPath);
        if (_azurite is not null) await _azurite.DisposeAsync();
    }
}
