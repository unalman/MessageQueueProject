using Contracts.Events;
using Contracts.Models;
using FluentAssertions;
using Messaging;
using System.Net;
using System.Net.Http.Json;

namespace OrderApi.Tests
{
    public class PurchaseEndpointTests : IClassFixture<OrderApiFactory>
    {
        private readonly HttpClient _client;

        public PurchaseEndpointTests(OrderApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Purchase_Should_Return_Success_When_Request_Is_Valid()
        {
            var request = new
            {
                userEmail = "test@test.com",
                items = new[]
                {
                    new
                    {
                        sku = "SKU-1",
                        quantity = 2
                    }
                },
                payment = new
                {
                    cardToken = "tok_123",
                    amount = 100
                }
            };
            var response = await _client.PostAsJsonAsync("/orders/purchase", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Purchase_Should_Return_BadRequest_When_Email_Is_Empty()
        {
            var request = new
            {
                userEmail = "",
                items = new[]
               {
                    new
                    {
                        sku = "SKU-1",
                        quantity = 2
                    }
                },
                payment = new
                {
                    cardToken = "tok_123",
                    amount = 100
                }
            };

            var response = await _client.PostAsJsonAsync("/orders/purchase", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Purchase_Should_Return_BadRequest_When_Items_Sku_Is_Invalid()
        {
            var request = new
            {
                userEmail = "test@test.com",
                items = new[]
                {
                    new
                    {
                        sku = "",
                        quantity = 2
                    }
                },
                payment = new
                {
                    cardToken = "tok_123",
                    amount = 100
                }
            };
            var response = await _client.PostAsJsonAsync("/orders/purchase", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Purchase_Should_Return_BadRequest_When_Items_Quantity_Is_Invalid()
        {
            var request = new
            {
                userEmail = "test@test.com",
                items = new[]
                {
                    new
                    {
                        sku = "SKU-1",
                        quantity = -1
                    }
                },
                payment = new
                {
                    cardToken = "tok_123",
                    amount = 100
                }
            };
            var response = await _client.PostAsJsonAsync("/orders/purchase", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Purchase_Should_Return_BadRequest_When_Items_Are_Empty()
        {
            var request = new
            {
                userEmail = "test@test.com",
                items = Array.Empty<object>(),
                payment = new
                {
                    cardToken = "tok_123",
                    amount = 100
                }
            };

            var response = await _client.PostAsJsonAsync("/orders/purchase", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Purchase_Should_Return_BadRequest_When_Payment_CardToken_Is_Invalid()
        {
            var request = new
            {
                userEmail = "test@test.com",
                items = new[]
              {
                    new
                    {
                        sku = "SKU-1",
                        quantity = 1
                    }
                },
                payment = new
                {
                    cardToken = "",
                    amount = 100
                }
            };
            var response = await _client.PostAsJsonAsync("/orders/purchase", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Purchase_Should_Return_BadRequest_When_Payment_Amount_Is_Invalid()
        {
            var request = new
            {
                userEmail = "test@test.com",
                items = new[]
                {
                    new
                    {
                        sku = "SKU-1",
                        quantity = 1
                    }
                },
                payment = new
                {
                    cardToken = "tok_123",
                    amount = -200
                }
            };
            var response = await _client.PostAsJsonAsync("/orders/purchase", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Purchase_Should_Return_BadRequest_When_Payment_Is_Null()
        {
            var request = new
            {
                userEmail = "test@test.com",
                items = new[]
              {
                    new
                    {
                        sku = "SKU-1",
                        quantity = 1
                    }
                },
            };

            var response = await _client.PostAsJsonAsync("/orders/purchase", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
