using ECommerceStoreInvoice.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Shouldly;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.Configuration;

public sealed class MongoConnectionConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("mongodb//localhost:27017")]
    public void MissingOrMalformedUriFailsBeforeConnecting(string? connectionString)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDbSettings:ConnectionString"] = connectionString
            })
            .Build();
        var services = new ServiceCollection();
        services.AddInfrastructure(config);
        using var provider = services.BuildServiceProvider();

        var exception = Should.Throw<InvalidOperationException>(() =>
            provider.GetRequiredService<IMongoClient>());

        exception.Message.ShouldContain("MongoDbSettings:ConnectionString");
        exception.Message.ShouldNotContain("localhost");
    }
}
