using Reqnroll;

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
            _factory = new ApplicationFactory(_scenarioContext.ScenarioInfo.Tags.Contains("products-api"));
            await _factory.InitializeAsync();
            _apiContext.Factory = _factory;
            _apiContext.HttpClient = _factory.CreateClient();
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
