using Reqnroll;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common
{
    [Binding]
    public sealed class TestHooks
    {
        private readonly ScenarioApiContext _apiContext;
        private ApplicationFactory? _factory;

        public TestHooks(ScenarioApiContext apiContext)
        {
            _apiContext = apiContext;
        }

        [BeforeScenario]
        public async Task BeforeScenario()
        {
            _factory = new ApplicationFactory();
            await _factory.InitializeAsync();
            _apiContext.HttpClient = _factory.CreateClient();
        }

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
