using Contracts.Events;
using Contracts.Models;
using FluentAssertions;
using Messaging;
using System.Net;
using System.Net.Http.Json;

namespace OrderApi.Tests
{
    public class PurchaseEndpointTests : IClassFixture<OrderApiFactory>/*, IDisposable*/
    {
        private readonly HttpClient _client;
        //private readonly OrderApiFactory _factory;

        public PurchaseEndpointTests(OrderApiFactory factory)
        {
            //_factory = factory;
            _client = factory.CreateClient();
        }

        public void Dispose()
        {
            //_factory.Publisher.Reset();
        }

        //[Fact]
        //public async Task Purchase_Should_Publish_OrderCreated_Event()
        //{
        //    var request = new
        //    {
        //        userEmail = "test@test.com",
        //        items = new[]
        //        {
        //            new
        //            {
        //                sku = "SKU-1",
        //                quantity = 2
        //            }
        //        },
        //        payment = new
        //        {
        //            cardToken = "tok_123",
        //            amount = 100
        //        }
        //    };

        //    var response = await _client.PostAsJsonAsync("/orders/purchase", request);

        //    //var body = await response.Content.ReadAsStringAsync();

        //    //Console.WriteLine(body);
        //    //Console.WriteLine(response.StatusCode);

        //    response.IsSuccessStatusCode.Should().BeTrue();

        //    _factory.Publisher.PublishedMessage
        //        .Should().BeOfType<OrderCreatedEvent>();

        //    _factory.Publisher.RoutingKey
        //        .Should().Be(MessagingConstants.OrderCreatedEventsRoutingKey);
        //}

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

        //[Fact]
        //public async Task Purchase_Should_Return_500_When_Publish_Fails()
        //{
        //    var factory = new FailingOrderApiFactory();

        //    var client = factory.CreateClient();

        //    var request = new
        //    {
        //        userEmail = "test@test.com",
        //        items = new[]
        //        {
        //            new
        //            {
        //                sku = "SKU-1",
        //                quantity = 1
        //            }
        //        },
        //        payment = new
        //        {
        //            cardToken = "tok_123",
        //            amount = 100
        //        }
        //    };
        //    var response = await client.PostAsJsonAsync("/orders/purchase", request);

        //    response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        //}
    }
}
