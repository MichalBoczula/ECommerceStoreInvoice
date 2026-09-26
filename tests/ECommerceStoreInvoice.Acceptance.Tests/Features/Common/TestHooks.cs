using Reqnroll;
using System.Text.Json;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common
{
    [Binding]
    public sealed class TestHooks
    {
        private readonly ScenarioApiContext _apiContext;
        private readonly ScenarioContext _scenarioContext;
        private ApplicationFactory? _factory;

        public TestHooks(ScenarioApiContext apiContext, ScenarioContext scenarioContext)
        {
            _apiContext = apiContext;
            _scenarioContext = scenarioContext;
        }

        [BeforeScenario]
        public async Task BeforeScenario()
        {
            _factory = new ApplicationFactory(
                _scenarioContext.ScenarioInfo.Tags.Contains("products-api"),
                _scenarioContext.ScenarioInfo.Tags.Contains("pdf-fails-once"),
                _scenarioContext.ScenarioInfo.Tags.Contains("completion-ack-lost"));
            await _factory.InitializeAsync();
            _apiContext.Factory = _factory;
            using var swaggerClient = _factory.CreateClient();
            var document = JsonDocument.Parse(await swaggerClient.GetStringAsync("/swagger/v1/swagger.json"));
            _apiContext.HttpClient = _factory.CreateDefaultClient(new OpenApiResponseHandler(document));
        }

        [AfterTestRun]
        public static Task AfterTestRun() => ApplicationFactory.DisposeSharedProductsAsync();

        [AfterScenario]
        public async Task AfterScenario()
        {
            _apiContext.HttpClient?.Dispose();

            if (_factory is not null)
            {
                await _factory.DisposeAsync();
            }
        }
    }
}
