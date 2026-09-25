using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Application.Services.Abstract.Orders;
using ECommerceStoreInvoice.Application.Services.Abstract.ShoppingCarts;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Reqnroll;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Common;

[Binding]
public sealed class UnexpectedServerErrorsSteps(ScenarioApiContext context) : IDisposable
{
    private const string SensitiveDetail = "INVOICE_INTERNAL_FAILURE_DO_NOT_EXPOSE";
    private readonly Guid _id = Guid.NewGuid();
    private WebApplicationFactory<Program>? _failingFactory;
    private HttpClient? _client;
    private string? _path;

    [Given("the {string} dependency fails unexpectedly")]
    public void GivenDependencyFails(string operationId)
    {
        _failingFactory = context.Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            var failure = new InvalidOperationException(SensitiveDetail);
            switch (operationId)
            {
                case "GetShoppingCartByClientId":
                case "CreateShoppingCart":
                case "UpdateShoppingCart":
                    var carts = new Mock<IShoppingCartService>();
                    carts.Setup(x => x.GetShoppingCartByClientId(It.IsAny<Guid>())).ThrowsAsync(failure);
                    carts.Setup(x => x.CreateShoppingCart(It.IsAny<Guid>())).ThrowsAsync(failure);
                    carts.Setup(x => x.UpdateShoppingCart(It.IsAny<Guid>(), It.IsAny<UpdateShoppingCartRequestDto>())).ThrowsAsync(failure);
                    Replace(services, carts);
                    break;
                case "GetOrderById":
                case "GetOrdersByClientId":
                case "CreateOrder":
                case "UpdateOrderStatus":
                    var orders = new Mock<IOrderService>();
                    orders.Setup(x => x.GetOrderByOrderId(It.IsAny<Guid>())).ThrowsAsync(failure);
                    orders.Setup(x => x.GetOrdersByClientId(It.IsAny<Guid>())).ThrowsAsync(failure);
                    orders.Setup(x => x.CreateOrder(It.IsAny<Guid>())).ThrowsAsync(failure);
                    orders.Setup(x => x.UpdateOrderStatus(It.IsAny<Guid>(), It.IsAny<UpdateOrderStatusRequestDto>())).ThrowsAsync(failure);
                    Replace(services, orders);
                    break;
                case "GetInvoiceById":
                case "CreateInvoiceForOrder":
                    var invoices = new Mock<IInvoiceService>();
                    invoices.Setup(x => x.GetInvoiceById(It.IsAny<Guid>())).ThrowsAsync(failure);
                    invoices.Setup(x => x.CreateInvoiceForOrder(It.IsAny<Guid>(), It.IsAny<Guid>())).ThrowsAsync(failure);
                    Replace(services, invoices);
                    break;
                case "GetClientDataVersionByClientId":
                case "CreateClientDataVersion":
                    var clientData = new Mock<IClientDataVersionService>();
                    clientData.Setup(x => x.GetByClientId(It.IsAny<Guid>())).ThrowsAsync(failure);
                    clientData.Setup(x => x.Create(It.IsAny<Guid>(), It.IsAny<CreateClientDataVersionRequestDto>())).ThrowsAsync(failure);
                    Replace(services, clientData);
                    break;
                case "GetFlowDocumentation":
                    var flows = new Mock<IShoppingCartDescriptorService>();
                    flows.Setup(x => x.GetShoppingCartByClientIdDescriptor()).Throws(failure);
                    Replace(services, flows);
                    break;
                case "GetValidationDocumentation":
                    services.RemoveAll<IValidationPolicyDescriptorProvider>();
                    var validations = new Mock<IValidationPolicyDescriptorProvider>();
                    validations.Setup(x => x.Describe()).Throws(failure);
                    services.AddSingleton(validations.Object);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operationId), operationId, "Unknown operation.");
            }
        }));
        _client = _failingFactory.CreateClient();
    }

    [When("I call the failing {string} endpoint")]
    public async Task WhenEndpointIsCalled(string operationId)
    {
        _client.ShouldNotBeNull();
        _path = operationId switch
        {
            "GetShoppingCartByClientId" => $"/shopping-carts/client/{_id}",
            "CreateShoppingCart" or "UpdateShoppingCart" => $"/shopping-carts/{_id}",
            "GetOrderById" => $"/orders/{_id}",
            "GetOrdersByClientId" => $"/orders/client/{_id}",
            "CreateOrder" => $"/orders/{_id}",
            "UpdateOrderStatus" => $"/orders/{_id}/status",
            "GetInvoiceById" => $"/invoices/{_id}",
            "CreateInvoiceForOrder" => $"/invoices/{_id}/{Guid.NewGuid()}",
            "GetClientDataVersionByClientId" => $"/client-data-versions/client/{_id}",
            "CreateClientDataVersion" => $"/client-data-versions/{_id}",
            "GetFlowDocumentation" => "/orders-documentation/flows",
            "GetValidationDocumentation" => "/orders-documentation/validations",
            _ => throw new ArgumentOutOfRangeException(nameof(operationId), operationId, "Unknown operation.")
        };

        context.Response = operationId switch
        {
            "CreateShoppingCart" or "CreateOrder" or "CreateInvoiceForOrder" => await _client.PostAsync(_path, null),
            "UpdateShoppingCart" => await _client.PutAsJsonAsync(_path, new UpdateShoppingCartRequestDto { Lines = [] }),
            "UpdateOrderStatus" => await _client.PatchAsJsonAsync(_path, new UpdateOrderStatusRequestDto { Status = "Paid" }),
            "CreateClientDataVersion" => await _client.PostAsJsonAsync(_path, new CreateClientDataVersionRequestDto
            {
                ClientName = "Example", PostalCode = "00-001", City = "Warsaw", Street = "Street",
                BuildingNumber = "1", ApartmentNumber = "1", PhoneNumber = "123456789",
                PhonePrefix = "48", AddressEmail = "example@example.com"
            }),
            _ => await _client.GetAsync(_path)
        };
    }

    [Then("the Invoice server error is safe")]
    public async Task ThenServerErrorIsSafe()
    {
        context.Response.ShouldNotBeNull();
        context.Response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        context.Response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var body = await context.Response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("status").GetInt32().ShouldBe(500);
        json.RootElement.GetProperty("title").GetString().ShouldBe("Server error.");
        json.RootElement.GetProperty("detail").GetString().ShouldBe("An unexpected error occurred.");
        json.RootElement.GetProperty("instance").GetString().ShouldBe(_path);
        json.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(SensitiveDetail);
        body.ShouldNotContain("InvalidOperationException");
        body.ShouldNotContain("stackTrace");
    }

    private static void Replace<T>(IServiceCollection services, Mock<T> mock) where T : class
    {
        services.RemoveAll<T>();
        services.AddSingleton(mock.Object);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _failingFactory?.Dispose();
    }
}
