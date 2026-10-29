using System.Text.Json;
using System.Text.Json.Nodes;
using ECommerceStoreInvoice.API.Configuration.Common;
using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.RequestsDto.Orders;
using ECommerceStoreInvoice.Application.Common.RequestsDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ClientDataVersions;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.Orders;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceStoreInvoice.API.Configuration;

/// <summary>Typed source examples, serialized with web naming; these are not a second schema.</summary>
public static class InvoiceContractExamples
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyDictionary<Type, object> Values = Create();

    public static JsonObject? For(Type type) => Values.TryGetValue(type, out var value)
        ? JsonSerializer.SerializeToNode(value, type, Options)!.AsObject() : null;

    private static Dictionary<Type, object> Create()
    {
        var id = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var client = Guid.Parse("22222222-2222-4222-8222-222222222222");
        var product = Guid.Parse("33333333-3333-4333-8333-333333333333");
        var created = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var snapshot = new ProductVersionResponseDto
        {
            Id = id,
            ProductId = product,
            IsActive = true,
            CreatedAt = created,
            DeactivatedAt = created.AddDays(2),
            PriceAmount = 12.50m,
            PriceCurrency = "PLN",
            Name = "Example.Phone",
            Brand = "Example"
        };
        var line = new OrderLineResponseDto { ProductVersionId = id, ProductVersion = snapshot, Quantity = 2, LineTotalAmount = 25.00m };
        var cartLine = new ShoppingCartLineResponseDto { ProductId = product, Quantity = 2 };
        var requestLine = new ShoppingCartLineRequestDto { ProductId = product, Quantity = 2 };
        var error = new ValidationError { Name = "ClientIdIsEmptyValidationRule", Message = "ClientId cannot be empty Guid.", Entity = "Client" };
        var step = new FlowStepDescriptor { Order = 1, StepName = "Validate client identifier", BpmnId = "validate-client-id" };
        var flow = new FlowDescriptor { FlowName = "Example source flow", Steps = [step] };
        var rule = new ValidationRuleDescriptor { RuleName = error.Name, Rules = [error] };
        var policy = new ValidationPolicyDescriptor { PolicyName = "ClientValidationPolicy", Rules = [rule] };
        return new()
        {
            [typeof(CreateClientDataVersionRequestDto)] = new CreateClientDataVersionRequestDto { ClientName = "Anna Example", PostalCode = "00-001", City = "Warsaw", Street = "Example.St", BuildingNumber = "10", ApartmentNumber = "5", PhoneNumber = "123456789", PhonePrefix = "48", AddressEmail = "anna@example.com" },
            [typeof(ClientDataVersionResponseDto)] = new ClientDataVersionResponseDto { Id = id, ClientId = client, CreatedAt = created, ClientName = "Anna Example", PostalCode = "00-001", City = "Warsaw", Street = "Example.St", BuildingNumber = "10", ApartmentNumber = "5", PhoneNumber = "123456789", PhonePrefix = "48", AddressEmail = "anna@example.com" },
            [typeof(ShoppingCartLineRequestDto)] = requestLine,
            [typeof(UpdateShoppingCartRequestDto)] = new UpdateShoppingCartRequestDto { Lines = [requestLine] },
            [typeof(UpdateOrderStatusRequestDto)] = new UpdateOrderStatusRequestDto { Status = "Paid" },
            [typeof(ShoppingCartLineResponseDto)] = cartLine,
            [typeof(ShoppingCartResponseDto)] = new ShoppingCartResponseDto { Id = id, ClientId = client, CreatedAt = created, UpdatedAt = created, Lines = [cartLine] },
            [typeof(ProductVersionResponseDto)] = snapshot,
            [typeof(OrderLineResponseDto)] = line,
            [typeof(OrderResponseDto)] = new OrderResponseDto { Id = id, ClientId = client, CreatedAt = created.AddHours(1), UpdatedAt = created.AddHours(2), Status = "Paid", TotalAmount = 25.00m, TotalCurrency = "PLN", Lines = [line] },
            [typeof(InvoiceResponseDto)] = new InvoiceResponseDto { Id = id, OrderId = product, ClietDataVersionId = client, StorageUrl = "https://example.blob.core.windows.net/invoices/example.pdf", CreatedAt = created.AddHours(3) },
            [typeof(FlowStepDescriptor)] = step,
            [typeof(FlowDescriptor)] = flow,
            [typeof(FlowDescriptorsResponseDto)] = new FlowDescriptorsResponseDto { Flows = [new() { ["ExampleFlowDescriptor"] = flow }] },
            [typeof(ValidationError)] = error,
            [typeof(ValidationRuleDescriptor)] = rule,
            [typeof(ValidationPolicyDescriptor)] = policy,
            [typeof(ValidationDescriptorsResponseDto)] = new ValidationDescriptorsResponseDto { Validations = [new() { [policy.PolicyName] = policy }] },
            [typeof(ApiProblemDetails)] = new ApiProblemDetails { Type = TypeUri("6.5.1"), Title = "Validation failed.", Status = 400, Detail = "One or more validation errors occurred.", Instance = "/shopping-carts/00000000-0000-0000-0000-000000000000", TraceId = "example-trace", Errors = [error] },
            [typeof(NotFoundProblemDetails)] = new NotFoundProblemDetails { Type = TypeUri("6.5.4"), Title = "Resource not found.", Status = 404, Detail = "The requested resource was not found.", Instance = $"/invoices/{id:D}", TraceId = "example-trace" },
            [typeof(ConflictProblemDetails)] = new ConflictProblemDetails { Type = TypeUri("6.5.8"), Title = "Conflict.", Status = 409, Detail = "Order was changed by another request. Reload it before trying again.", Instance = $"/orders/{id:D}/status", TraceId = "example-trace" },
            [typeof(ProblemDetails)] = new ProblemDetails { Type = TypeUri("6.6.1"), Title = "Server error.", Status = 500, Detail = "An unexpected error occurred.", Instance = $"/orders/{id:D}", Extensions = { ["traceId"] = "example-trace" } }
        };
    }

    private static string TypeUri(string section) => "https://datatracker.ietf.org/doc/html/rfc7231#section-" + section;
}
