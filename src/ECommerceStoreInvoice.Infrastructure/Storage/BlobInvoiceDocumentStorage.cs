using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ECommerceStoreInvoice.Domain.AggregatesModel.InvoiceAggregate.Repositories;
using System.Text.RegularExpressions;

namespace ECommerceStoreInvoice.Infrastructure.Storage;

internal sealed class BlobInvoiceDocumentStorage(BlobContainerClient container) : IInvoiceDocumentStorage
{
    public async Task<string> UploadAsync(Guid invoiceId, Guid attemptId, Stream content, CancellationToken cancellationToken = default)
    {
        var blob = container.GetBlobClient(BlobName(invoiceId, attemptId));
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "application/pdf" },
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
        }, cancellationToken);
        return blob.Uri.AbsoluteUri;
    }

    public async Task DeleteAsync(Guid invoiceId, Guid attemptId, CancellationToken cancellationToken = default)
    {
        await container.GetBlobClient(BlobName(invoiceId, attemptId))
            .DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(string storageUrl, CancellationToken cancellationToken = default)
    {
        var prefix = container.Uri.AbsoluteUri.TrimEnd('/') + "/";
        if (!storageUrl.StartsWith(prefix, StringComparison.Ordinal)) return null;
        var name = storageUrl[prefix.Length..];
        // Read only attempt PDFs in this container; never follow arbitrary URLs.
        if (!Regex.IsMatch(name, "^[a-f0-9]{32}-[a-f0-9]{32}\\.pdf$")) return null;
        try
        {
            var download = await container.GetBlobClient(name).DownloadStreamingAsync(cancellationToken: cancellationToken);
            return download.Value.Content;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    private static string BlobName(Guid invoiceId, Guid attemptId) => $"{invoiceId:N}-{attemptId:N}.pdf";
}
