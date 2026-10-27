using System.Net;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common;

[Binding]
public sealed class ApiAvailabilitySteps(ScenarioApiContext context)
{
    [Given("the Invoice API is available")]
    public async Task GivenAvailable()
    {
        using var response = await context.HttpClient.GetAsync("/health/live");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }
}
