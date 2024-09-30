using MassTransit;
using Stocks.Api.Orders;
using Stocks.Api.Stocks;
using Stocks.Platform.Contracts;

namespace Stocks.Api;

internal static class Endpoints
{
    internal static void MapEndpoints(this IEndpointRouteBuilder app)
    {

        app.MapGet("stocks/{ticker}", async (string ticker, StocksClient stocksClient) =>
        {
            var stockPriceResponse = await stocksClient.GetDataForTicker(ticker);

            return stockPriceResponse is not null ? Results.Ok(stockPriceResponse) : Results.NotFound();
        });

        app.MapPost("stocks", async (
            PurchaseOrderRequest request,
            IPublishEndpoint publishEndpoint,
            ILogger<Program> logger) =>
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                Ticker = request.Ticker,
                LimitPrice = request.LimitPrice,
                Quantity = request.Quantity
            };

            OrdersDb.Instance.TryAdd(order.Id, order);

            logger.LogInformation("Created purchase order {@Order}", order);

            await publishEndpoint.Publish(new PurchaseOrderSent(order.Id));

            logger.LogInformation("Published purchase order sent {OrderId}", order.Id);

            return Results.Ok(order);
        });

        app.MapGet("orders/{id}", (Guid id) =>
        {
            var order = OrdersDb.Instance.GetValueOrDefault(id);

            return order is not null ? Results.Ok(order) : Results.NotFound();
        });
    }
}
