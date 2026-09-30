using System.Net;
using System.Net.Http.Json;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Invoices;

[Binding]
public sealed class GetInvoiceByOrderIdSteps(ScenarioApiContext context)
{
    private Guid _orderId;
    private Guid _invoiceId;

    [Given("an invoice lookup fixture in state {string}")]
    public async Task GivenFixture(string state)
    {
        _orderId = state == "empty" ? Guid.Empty : Guid.NewGuid();
        _invoiceId = Guid.NewGuid();
        if (state is not "missing" and not "empty")
            await context.Factory.SeedInvoiceLookupAsync(_invoiceId, _orderId, state);
    }

    [When("I retrieve the invoice by its order id")]
    public async Task WhenLookup()
    {
        context.Response = await context.HttpClient.GetAsync($"/invoices/by-order/{_orderId}");
    }

    [Then("invoice order lookup returns status {int}")]
    public async Task ThenStatus(int status)
    {
        context.Response.ShouldNotBeNull();
        context.Response.StatusCode.ShouldBe((HttpStatusCode)status);
        if (status == 200)
        {
            var invoice = await context.Response.Content.ReadFromJsonAsync<InvoiceResponseDto>();
            invoice.ShouldNotBeNull();
            invoice.Id.ShouldBe(_invoiceId);
            invoice.OrderId.ShouldBe(_orderId);
            invoice.StorageUrl.ShouldBe("file:///invoices/lookup.pdf");
        }
        else
        {
            context.Response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        }
    }
}
