using Microsoft.Extensions.Logging.Abstractions;
using Ubiq.Http;
using Ubiq.SportXAPI;
using Ubiq.Definitions.Market;
using Ubiq.Extensions.Newtonsoft;
using Newtonsoft.Json;


HttpClient httpClient = HttpUtil.CreateHttpClient(false, false, false);
var httpClientHelper = new HttpClientHelper(NullLogger<HttpClientHelper>.Instance, new JsonSerializerSettings().Configure());

string privateKey = "<your key>";
string apiKey = "<your api key>";

var sx = new SportXAPI(NullLogger<SportXAPI>.Instance, httpClientHelper, httpClient, new Uri("https://api.sx.bet/"), privateKey, 4.0m, 2.0m, apiKey);

sx.OrdersUpdated += Sx_OrdersUpdated;
sx.MyTradeUpdated += Sx_MyTradeUpdated;

void Sx_MyTradeUpdated(object sender, TradeUpdateMessage e)
{
    Console.WriteLine(JsonConvert.SerializeObject(e, Formatting.Indented));
}

void Sx_OrdersUpdated(object sender, OrderUpdateMessage[] e)
{
    Console.WriteLine(JsonConvert.SerializeObject(e, Formatting.Indented));
}

await sx.Initialise();
sx.InitialiseWebSocket();

var markets = await sx.GetMarkets(new[] { Sport.Hockey });

var moneylineMarkets = markets.data.markets.Where(m => m.MarketType == MarketType.MoneylineIncludingOvertime).ToArray();
Market marketToBet = moneylineMarkets.OrderByDescending(m => m.StartTime).FirstOrDefault();

NewOrdersResponse result = await sx.PlaceOrders(new[]
{
    new PlaceOrder
    {
         Amount = new Amount(20m, "USDC"),
         Expiry = DateTime.UtcNow.AddHours(1),
         MarketHash = marketToBet.marketHash,
         Outcome1 = true,
         Price = new Price(PriceFormat.Decimal, 50m),
    },
});

CancelOrdersV2Response cancelResult = await sx.CancelOrdersV2(result.data.orders);

Console.ReadLine();
