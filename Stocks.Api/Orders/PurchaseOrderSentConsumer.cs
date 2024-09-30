using MassTransit;
using Stocks.Api.Stocks;
using Stocks.Platform.Contracts;

namespace Stocks.Api.Orders;

public class PurchaseOrderSentConsumer(StocksClient stocksClient, ILogger<PurchaseOrderSentConsumer> logger)
    : IConsumer<PurchaseOrderSent>
{
    public async Task Consume(ConsumeContext<PurchaseOrderSent> context)
    {
        logger.LogInformation("Processing purchase order {OrderId}", context.Message.OrderId);

        var order = OrdersDb.Instance.GetValueOrDefault(context.Message.OrderId);

        if (order is null)
        {
            logger.LogInformation("Couldn't find purchase order {OrderId}", context.Message.OrderId);

            return;
        }

        var stockPriceResponse = await stocksClient.GetDataForTicker(order.Ticker);

        var lastPrice = decimal.Parse(stockPriceResponse!.Price.High);

        if (lastPrice > order.LimitPrice)
        {
            logger.LogInformation("Couldn't process purchase order {@Order}, {LastPrice}", order, lastPrice);
            return;
        }

        logger.LogInformation("Processed purchase order {OrderId}", context.Message.OrderId);

        order.Filled = true;
        order.Price = lastPrice;

        logger.LogInformation("Publishing order filled for order {OrderId}", context.Message.OrderId);

        await context.Publish(new OrderFilled(order.Id));
    }
}
