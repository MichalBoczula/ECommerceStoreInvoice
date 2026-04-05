using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto;
using ECommerceStoreInvoice.Application.Services.Abstract.Invoices;
using ECommerceStoreInvoice.Application.Services.Abstract.Orders;
using ECommerceStoreInvoice.Application.Services.Abstract.ProductVersions;
using ECommerceStoreInvoice.Application.Services.Abstract.ShoppingCarts;
using ECommerceStoreInvoice.Application.Services.Abstract.ClientDataVersions;
using ECommerceStoreInvoice.Domain.Validation.Abstract;
using ECommerceStoreInvoice.Domain.Validation.Common;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceStoreInvoice.API.Endpoints
{
    public static class DocumentationEndpoints
    {
        public static IEndpointRouteBuilder MapDocumentationEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/documentation").WithTags("Documentation");

            MapFlowDocumentation(group);
            MapValidationDocumentation(group);

            return group;
        }

        private static void MapFlowDocumentation(IEndpointRouteBuilder group)
        {
            group.MapGet("/flows", (IShoppingCartDescriptorService shoppingCartDescriptor, IOrderDescriptorService orderDescriptor, IProductVersionDescriptorService productVersionDescriptor, IInvoiceDescriptorService invoiceDescriptor, IClientDataVersionDescriptorService clientDataVersionDescriptor) =>
            {
                var response = new FlowDescriptorsResponseDto
                {
                    Flows =
                    [
                        new Dictionary<string, FlowDescriptor>
                        {
                            [nameof(shoppingCartDescriptor.GetShoppingCartByClientIdDescriptor)] = shoppingCartDescriptor.GetShoppingCartByClientIdDescriptor()
                        },
                        new Dictionary<string, FlowDescriptor>
                        {
                            [nameof(shoppingCartDescriptor.GetUpdateShoppingCartDescriptor)] = shoppingCartDescriptor.GetUpdateShoppingCartDescriptor()
                        },
                        new Dictionary<string, FlowDescriptor>
                        {
                            [nameof(shoppingCartDescriptor.GetCreateShoppingCartDescriptor)] = shoppingCartDescriptor.GetCreateShoppingCartDescriptor()
                        },
                        new Dictionary<string, FlowDescriptor>
                        {
                            [nameof(orderDescriptor.GetOrderByIdDescriptor)] = orderDescriptor.GetOrderByIdDescriptor()
                        },
                        new Dictionary<string, FlowDescriptor>
                        {
                            [nameof(orderDescriptor.GetOrdersByClientIdDescriptor)] = orderDescriptor.GetOrdersByClientIdDescriptor()
                        },
                        new Dictionary<string, FlowDescriptor>
                        {
                            [nameof(orderDescriptor.GetCreateOrderDescriptor)] = orderDescriptor.GetCreateOrderDescriptor()
                        },
                        new Dictionary<string, FlowDescriptor>
                        {
                            [nameof(orderDescriptor.GetUpdateOrderStatusDescriptor)] = orderDescriptor.GetUpdateOrderStatusDescriptor()
                        },
                        new Dictionary<string, FlowDescriptor>
                        {
                            [nameof(productVersionDescriptor.GetProductVersionByIdDescriptor)] = productVersionDescriptor.GetProductVersionByIdDescriptor()
                        },
                        new Dictionary<string, FlowDescriptor>
                        {
                            [nameof(productVersionDescriptor.GetCreateProductVersionDescriptor)] = productVersionDescriptor.GetCreateProductVersionDescriptor()
                        },
                        new Dictionary<string, FlowDescriptor>
                        {
                            [nameof(invoiceDescriptor.GetInvoiceByIdDescriptor)] = invoiceDescriptor.GetInvoiceByIdDescriptor()
                        },
                        new Dictionary<string, FlowDescriptor>
                        {
                            [nameof(invoiceDescriptor.GetCreateInvoiceForOrderDescriptor)] = invoiceDescriptor.GetCreateInvoiceForOrderDescriptor()
                        },
                        new Dictionary<string, FlowDescriptor>
                        {
                            [nameof(clientDataVersionDescriptor.GetClientDataVersionByClientIdDescriptor)] = clientDataVersionDescriptor.GetClientDataVersionByClientIdDescriptor()
                        },
                        new Dictionary<string, FlowDescriptor>
                        {
                            [nameof(clientDataVersionDescriptor.GetCreateClientDataVersionDescriptor)] = clientDataVersionDescriptor.GetCreateClientDataVersionDescriptor()
                        },
                    ]
                };

                return Results.Ok(response);
            })
            .WithSummary("Get flow documentation.")
            .WithDescription("Returns flow descriptors mapped by descriptor name.")
            .WithName("GetFlowDocumentation")
            .Produces<FlowDescriptorsResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }

        private static void MapValidationDocumentation(IEndpointRouteBuilder group)
        {
            group.MapGet("/validations", (IEnumerable<IValidationPolicyDescriptorProvider> validationDescriptorProviders) =>
            {
                var validationDescriptors = validationDescriptorProviders
                    .Select(provider => provider.Describe())
                    .Select(descriptor => new Dictionary<string, ValidationPolicyDescriptor>
                    {
                        [descriptor.PolicyName] = descriptor
                    })
                    .ToList();

                var response = new ValidationDescriptorsResponseDto
                {
                    Validations = validationDescriptors
                };

                return Results.Ok(response);
            })
            .WithSummary("Get validation documentation.")
            .WithDescription("Returns validation descriptors mapped by policy name.")
            .WithName("GetValidationDocumentation")
            .Produces<ValidationDescriptorsResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        }
    }
}
