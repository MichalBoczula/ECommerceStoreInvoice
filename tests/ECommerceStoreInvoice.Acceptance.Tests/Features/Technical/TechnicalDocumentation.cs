using System.Net;
using System.Text.Json;
using ECommerceStoreInvoice.Acceptance.Tests.Features.Common;
using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Application.Services.Abstract.Orders;
using ECommerceStoreInvoice.Application.Services.Abstract.ProductVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.ShoppingCarts;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ECommerceStoreInvoice.Acceptance.Tests.Features.Technical;

internal static class TechnicalDocumentation
{
    private static readonly (Type Service, string[] Methods)[] Flows =
    [
        (typeof(IShoppingCartDescriptorService), ["GetShoppingCartByClientIdDescriptor", "GetUpdateShoppingCartDescriptor", "GetCreateShoppingCartDescriptor"]),
        (typeof(IOrderDescriptorService), ["GetOrderByIdDescriptor", "GetOrdersByClientIdDescriptor", "GetCreateOrderDescriptor", "GetUpdateOrderStatusDescriptor"]),
        (typeof(IProductVersionDescriptorService), ["GetProductVersionByIdDescriptor", "GetCreateProductVersionDescriptor"]),
        (typeof(IInvoiceDescriptorService), ["GetInvoiceByOrderIdDescriptor", "GetInvoiceByIdDescriptor", "GetDownloadInvoicePdfDescriptor", "GetCreateInvoiceForOrderDescriptor"]),
        (typeof(IClientDataVersionDescriptorService), ["GetClientDataVersionByClientIdDescriptor", "GetCreateClientDataVersionDescriptor"])
    ];
    private static readonly string[] Policies = ["ShoppingCartLineValidationPolicy", "ClientValidationPolicy", "ClientDataVersionValidationPolicy", "OrderValidationPolicy", "UpdateOrderValidationPolicy", "InvoiceValidationPolicy", "ProductVersionValidationPolicy", "GuidCollectionValidationPolicy"];

    internal static async Task AssertAsync(ScenarioApiContext context, HttpResponseMessage response, string kind)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        using var actual = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        using var scope = context.Factory.Services.CreateScope();
        object expected;
        if (kind == "flows")
        {
            var descriptors = new List<Dictionary<string, FlowDescriptor>>();
            foreach (var (type, methods) in Flows)
            {
                var service = scope.ServiceProvider.GetRequiredService(type);
                foreach (var name in methods)
                {
                    var descriptor = (FlowDescriptor)type.GetMethod(name)!.Invoke(service, null)!;
                    descriptor.FlowName.ShouldNotBeNullOrWhiteSpace(); descriptor.Steps.ShouldNotBeEmpty();
                    descriptor.Steps.Select(s => s.Order).ShouldBe(descriptor.Steps.Select(s => s.Order).Order());
                    descriptor.Steps.Select(s => s.Order).Distinct().Count().ShouldBe(descriptor.Steps.Count);
                    descriptor.Steps.ShouldAllBe(s => s.Order > 0 && !string.IsNullOrWhiteSpace(s.StepName) && !string.IsNullOrWhiteSpace(s.BpmnId));
                    descriptors.Add(new() { [name] = descriptor });
                }
            }
            descriptors.Count.ShouldBe(15);
            expected = new FlowDescriptorsResponseDto { Flows = descriptors };
        }
        else
        {
            kind.ShouldBe("validations");
            var descriptors = scope.ServiceProvider.GetServices<IValidationPolicyDescriptorProvider>().Select(provider => provider.Describe()).ToArray();
            descriptors.Select(d => d.PolicyName).ShouldBe(Policies);
            descriptors.ShouldAllBe(d => d.Rules.Count > 0);
            foreach (var policy in descriptors)
                foreach (var rule in policy.Rules)
                {
                    rule.RuleName.ShouldNotBeNullOrWhiteSpace(); rule.Rules.ShouldNotBeEmpty();
                    rule.Rules.ShouldAllBe(error => !string.IsNullOrWhiteSpace(error.Name) && !string.IsNullOrWhiteSpace(error.Message) && !string.IsNullOrWhiteSpace(error.Entity));
                }
            expected = new ValidationDescriptorsResponseDto { Validations = descriptors.Select(d => new Dictionary<string, ECommerceStoreInvoice.Domain.Validation.Common.ValidationPolicyDescriptor> { [d.PolicyName] = d }).ToList() };
        }
        var complete = JsonSerializer.SerializeToElement(expected, context.JsonOptions);
        JsonElement.DeepEquals(actual.RootElement, complete).ShouldBeTrue();
        context.RecordEvidence("Technical complete documentation source comparison", new { kind, expected = complete, actual = actual.RootElement.Clone(), completeSourceEqual = true });
    }
}
