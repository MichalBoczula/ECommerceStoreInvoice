using ECommerceStoreInvoice.Infrastructure.Configuration;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerceStoreInvoice.Acceptance.Tests;

public sealed class OpenApiExportTests
{
    [Fact]
    public async Task ExportGeneratedOpenApiWithoutMongo()
    {
        var initializerResolutions = 0;
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("OpenApiExport");
            builder.UseSetting("MongoDbSettings:ConnectionString", "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=250");
            builder.ConfigureTestServices(services => services.AddScoped<MongoInitializer>(_ =>
            {
                Interlocked.Increment(ref initializerResolutions);
                throw new InvalidOperationException("Export must not resolve Mongo initialization.");
            }));
        });
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        var openApi = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(openApi);
        _ = Features.Common.OpenApiContractAssertions.Check(document);
        var operations = document.RootElement.GetProperty("paths").EnumerateObject().SelectMany(path => path.Value.EnumerateObject())
            .Where(operation => operation.Value.ValueKind == JsonValueKind.Object && operation.Value.TryGetProperty("operationId", out _))
            .Select(operation => operation.Value.GetProperty("operationId").GetString()).ToArray();
        Assert.Equal(15, operations.Length);
        Assert.Equal(15, operations.Distinct().Count());

        var exportPath = Environment.GetEnvironmentVariable("OPENAPI_EXPORT_PATH");
        var exportDirectory = string.IsNullOrWhiteSpace(exportPath) ? null : Path.GetDirectoryName(Path.GetFullPath(exportPath))!;
        if (exportPath is not null && exportDirectory is not null) await File.WriteAllTextAsync(exportPath, openApi);
        foreach (var (route, fileName, collection, count) in new[]
        {
            ("/orders-documentation/flows", "flows.json", "flows", 15),
            ("/orders-documentation/validations", "validation-policies.json", "validations", 8)
        })
        {
            using var documentationResponse = await client.GetAsync(route);
            documentationResponse.EnsureSuccessStatusCode();
            var documentation = await documentationResponse.Content.ReadAsStringAsync();
            using var structured = JsonDocument.Parse(documentation);
            Assert.Equal(count, structured.RootElement.GetProperty(collection).GetArrayLength());
            if (exportDirectory is not null) await File.WriteAllTextAsync(Path.Combine(exportDirectory, fileName), documentation);
        }
        Assert.Equal(0, initializerResolutions);
    }
}
