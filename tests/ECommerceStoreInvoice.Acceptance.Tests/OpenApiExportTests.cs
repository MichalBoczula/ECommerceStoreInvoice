using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerceStoreInvoice.Acceptance.Tests;

public sealed class OpenApiExportTests
{
    [Fact]
    public async Task ExportGeneratedOpenApiWithoutMongo()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("OpenApiExport"));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        var openApi = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(openApi);
        Assert.True(document.RootElement.GetProperty("paths").EnumerateObject().Any());

        var exportPath = Environment.GetEnvironmentVariable("OPENAPI_EXPORT_PATH");
        if (!string.IsNullOrWhiteSpace(exportPath))
            await File.WriteAllTextAsync(exportPath, openApi);
    }
}
