using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace Stocks.Api.Stocks;

public sealed class StocksClient(
    HttpClient httpClient,
    IConfiguration configuration,
    IMemoryCache memoryCache,
    ILogger<StocksClient> logger)
{
    public async Task<StockPriceResponse?> GetDataForTicker(string ticker)
    {
        logger.LogInformation("Getting stock price information for {Ticker}", ticker);

        var stockPriceResponse = await memoryCache.GetOrCreateAsync($"stocks-{ticker}", async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            return await GetStockPrice(ticker);
        });

        if (stockPriceResponse is null)
        {
            logger.LogWarning("Failed to get stock price information for {Ticker}", ticker);
        }
        else
        {
            logger.LogInformation(
                "Completed getting stock price information for {Ticker}, {@Stock}",
                ticker,
                stockPriceResponse);
        }

        return stockPriceResponse;
    }

    private async Task<StockPriceResponse?> GetStockPrice(string ticker)
    {
        var tickerDataString = await httpClient.GetStringAsync(
            $"?function=TIME_SERIES_INTRADAY&symbol={ticker}&interval=15min&apikey={configuration["StocksApiKey"]}");

        var tickerData = JsonConvert.DeserializeObject<AlphaVantageData>(tickerDataString);

        var lastPrice = tickerData?.TimeSeries.FirstOrDefault().Value;

        if (lastPrice is null)
        {
            return null;
        }

        return new StockPriceResponse(ticker, lastPrice);
    }
}
