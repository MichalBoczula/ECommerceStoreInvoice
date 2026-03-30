using ECommerceStoreInvoice.Application.Common.FlowDescriptors;
using ECommerceStoreInvoice.Application.Common.ResponsesDto.ShoppingCarts;
using ECommerceStoreInvoice.Application.Services.Abstract.ShoppingCarts;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceStoreInvoice.API.Endpoints
{
    public static class ShoppingCartFlowDescriptorsEndpoints
    {
        public static IEndpointRouteBuilder MapShoppingCartFlowDescriptorsEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/shopping-carts/flow-descriptors").WithTags("Shopping Cart Flows");

            group.MapGet("/", (IShoppingCartDescriptorService descriptor) =>
            {
                var response = new ShoppingCartFlowsResponseDto
                {
                    ShoppingCartFlows =
                    [
                        new Dictionary<string, FlowDescriptor>
                        {
                            [nameof(descriptor.GetShoppingCartByClientIdDescriptor)] = descriptor.GetShoppingCartByClientIdDescriptor()
                        },
                        new Dictionary<string, FlowDescriptor>
                        {
                            [nameof(descriptor.GetUpdateShoppingCartDescriptor)] = descriptor.GetUpdateShoppingCartDescriptor()
                        }
                    ]
                };

                return Results.Ok(response);
            })
            .WithSummary("Get shopping cart flow descriptors.")
            .WithDescription("Returns shopping cart flow descriptors mapped by descriptor name.")
            .WithName("GetShoppingCartFlowDescriptors")
            .Produces<ShoppingCartFlowsResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
