using Azure;
using Azure.Storage.Blobs;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using ECommerceStoreInvoice.Infrastructure.Storage;
using Shouldly;
using System.Security.Cryptography;
using System.Text;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Storage;

public sealed class BlobInvoiceDocumentStorageTests : IAsyncLifetime
{
    private readonly string _key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private IContainer _azurite = null!;
    private BlobContainerClient _container = null!;

    public async Task InitializeAsync()
    {
        _azurite = new ContainerBuilder("mcr.microsoft.com/azure-storage/azurite:3.35.0")
            .WithEnvironment("AZURITE_ACCOUNTS", $"invoicetest:{_key}")
            .WithCommand("azurite-blob", "--blobHost", "0.0.0.0", "--skipApiVersionCheck")
            .WithPortBinding(10000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(
                "Azurite Blob service successfully listens on", options => options.WithTimeout(TimeSpan.FromSeconds(60))))
            .Build();
        await _azurite.StartAsync();
        var connection = $"DefaultEndpointsProtocol=http;AccountName=invoicetest;AccountKey={_key};BlobEndpoint=http://{_azurite.Hostname}:{_azurite.GetMappedPublicPort(10000)}/invoicetest;";
        _container = new BlobContainerClient(connection, "invoices");
        await _container.CreateAsync();
    }

    [Fact]
    public async Task PdfSurvivesAdapterReplacement_AndCleanupDeletesOnlyItsAttempt()
    {
        var invoiceId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var storage = new BlobInvoiceDocumentStorage(_container);
        await using var content = new MemoryStream("%PDF-1.7\nPersistent invoice\n%%EOF"u8.ToArray());
        var url = await storage.UploadAsync(invoiceId, attemptId, content);
        url.ShouldEndWith($"/{invoiceId:N}-{attemptId:N}.pdf");
        url.ShouldNotContain("?");
        (await _container.GetBlobClient($"{invoiceId:N}-{attemptId:N}.pdf").GetPropertiesAsync()).Value.ContentType.ShouldBe("application/pdf");
        using var anonymous = new HttpClient();
        (await anonymous.GetAsync(url)).IsSuccessStatusCode.ShouldBeFalse();

        var restartedStorage = new BlobInvoiceDocumentStorage(_container);
        await using var download = await restartedStorage.OpenReadAsync(url);
        download.ShouldNotBeNull();
        using var reader = new StreamReader(download);
        (await reader.ReadToEndAsync()).ShouldBe(Encoding.UTF8.GetString(content.ToArray()));

        await storage.DeleteAsync(invoiceId, Guid.NewGuid());
        await using (var preserved = await storage.OpenReadAsync(url)) preserved.ShouldNotBeNull();
        await storage.DeleteAsync(invoiceId, attemptId);
        await storage.DeleteAsync(invoiceId, attemptId);
        (await storage.OpenReadAsync(url)).ShouldBeNull();
    }

    [Fact]
    public async Task ExistingAttemptCannotBeOverwritten()
    {
        var storage = new BlobInvoiceDocumentStorage(_container);
        var invoiceId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        await storage.UploadAsync(invoiceId, attemptId, new MemoryStream("%PDF-original"u8.ToArray()));
        await Should.ThrowAsync<RequestFailedException>(() =>
            storage.UploadAsync(invoiceId, attemptId, new MemoryStream("%PDF-replacement"u8.ToArray())));
    }

    [Theory]
    [InlineData("https://other.example/invoices/document.pdf")]
    [InlineData("file:///etc/passwd")]
    public async Task OtherStorageLocationsAreNotRead(string location)
    {
        (await new BlobInvoiceDocumentStorage(_container).OpenReadAsync(location)).ShouldBeNull();
    }

    public async Task DisposeAsync() => await _azurite.DisposeAsync();
}
