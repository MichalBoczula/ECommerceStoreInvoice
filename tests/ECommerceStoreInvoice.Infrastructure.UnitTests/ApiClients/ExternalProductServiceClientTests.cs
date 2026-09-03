using System.Net;
using ECommerceStoreInvoice.Infrastructure.ApiClients.Concret.Products;
using ECommerceStoreInvoice.Infrastructure.ApiClients.Products;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Moq;
using Moq.Protected;
using Shouldly;

namespace ECommerceStoreInvoice.Infrastructure.UnitTests.ApiClients
{
    public sealed class ExternalProductServiceClientTests
    {
        private ExternalProductServiceClient CreateSut(HttpResponseMessage mockedResponse)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockedResponse);

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:5000")
            };

            var authProvider = new AnonymousAuthenticationProvider();
            var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
            var kiotaClient = new ProductApiClient(adapter);

            return new ExternalProductServiceClient(kiotaClient);
        }

        [Fact]
        public async Task GetProductsByIds_ShouldReturnParsedProducts_WhenApiReturns200()
        {
            var productId = Guid.NewGuid();
            var requestIds = new[] { productId };

            var jsonResponse = $@"
            [
                {{
                    ""id"": ""{productId}"",
                    ""name"": ""iPhone 15"",
                    ""brand"": ""Apple"",
                    ""price"": {{
                        ""amount"": 4500.50,
                        ""currency"": ""PLN""
                    }}
                }}
            ]";

            var mockedResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
            };

            var sut = CreateSut(mockedResponse);

            // act
            var result = await sut.GetProductsByIds(requestIds);

            // assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);

            var product = result.First();
            product.ProductId.ShouldBe(productId);
            product.Name.ShouldBe("iPhone 15");
            product.Brand.ShouldBe("Apple");
            product.Price.Amount.ShouldBe(4500.50m);
            product.Price.Currency.ShouldBe("PLN");
        }

        [Fact]
        public async Task GetProductsByIds_ShouldReturnEmpty_WhenApiReturns404()
        {
            // arrange
            var requestIds = new[] { Guid.NewGuid() };

            var mockedResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };

            var sut = CreateSut(mockedResponse);

            // act
            var result = await sut.GetProductsByIds(requestIds);

            // assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }
    }
}