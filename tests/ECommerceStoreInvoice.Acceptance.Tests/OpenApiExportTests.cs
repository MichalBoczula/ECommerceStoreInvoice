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
        {
            await File.WriteAllTextAsync(exportPath, openApi);

            var exportDirectory = Path.GetDirectoryName(Path.GetFullPath(exportPath))!;
            foreach (var (route, fileName, collection) in new[]
            {
                ("/orders-documentation/flows", "flows.json", "flows"),
                ("/orders-documentation/validations", "validation-policies.json", "validations")
            })
            {
                using var documentationResponse = await client.GetAsync(route);
                documentationResponse.EnsureSuccessStatusCode();
                var documentation = await documentationResponse.Content.ReadAsStringAsync();
                using var structured = JsonDocument.Parse(documentation);
                Assert.NotEmpty(structured.RootElement.GetProperty(collection).EnumerateArray());
                await File.WriteAllTextAsync(Path.Combine(exportDirectory, fileName), documentation);
            }
        }
    }
}
