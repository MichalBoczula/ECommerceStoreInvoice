using System.Net;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Invoices;

[Binding]
public sealed class DownloadInvoicePdfSteps(ScenarioApiContext context)
{
    private Guid _invoiceId;

    [Given("an invoice PDF fixture in state {string}")]
    public async Task GivenFixture(string state)
    {
        _invoiceId = state == "empty" ? Guid.Empty : Guid.NewGuid();
        if (state is not "missing" and not "empty")
            await context.Factory.SeedDownloadInvoiceAsync(_invoiceId, state);
    }

    [When("I download the invoice PDF")]
    public async Task WhenDownload() =>
        context.Response = await context.HttpClient.GetAsync($"/invoices/{_invoiceId}/pdf");

    [Then("invoice PDF download returns status {int}")]
    public async Task ThenStatus(int status)
    {
        context.Response.ShouldNotBeNull();
        context.Response.StatusCode.ShouldBe((HttpStatusCode)status);
        if (status == 200)
        {
            context.Response.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
            context.Response.Content.Headers.ContentDisposition!.FileNameStar.ShouldBe($"invoice-{_invoiceId:D}.pdf");
            (await context.Response.Content.ReadAsStringAsync()).ShouldStartWith("%PDF-");
            context.Response.Headers.CacheControl!.NoStore.ShouldBeTrue();
        }
        else
        {
            context.Response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
            (await context.Response.Content.ReadAsStringAsync()).ShouldNotContain("Simulated private PDF download failure");
        }
    }
}
