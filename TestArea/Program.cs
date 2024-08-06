using Microsoft.Extensions.Logging.Abstractions;
using Ubiq.Http;
using Ubiq.SportXAPI;
using Ubiq.Definitions.Market;
using Ubiq.Extensions.Newtonsoft;
using Newtonsoft.Json;
using Ubiq.Extensions;


HttpClient httpClient = HttpUtil.CreateHttpClient(false, false, false);
var httpClientHelper = new HttpClientHelper(NullLogger<HttpClientHelper>.Instance, new JsonSerializerSettings().Configure());

string privateKey = "";
string apiKey = "";

var sx = new SportXAPI(NullLogger<SportXAPI>.Instance, httpClientHelper, httpClient, new Uri("https://api.sx.bet/"), privateKey, 4.0m, 2.0m, apiKey);

sx.OrdersUpdated += Sx_OrdersUpdated;
sx.MyTradeUpdated += Sx_MyTradeUpdated;
sx.MarketsUpdated += Sx_MarketsUpdated;

void Sx_MarketsUpdated(object sender, MarketUpdateMessage[] e)
{
    var inactive = e.Where(m => m.status == "INACTIVE").ToArray();
    if (inactive.Length > 0)
    {
        Int32 i = 0;
    }
}

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

var markets = await sx.GetMarkets([Sport.Baseball, Sport.Basketball, Sport.Hockey, Sport.Soccer, Sport.Tennis]);

//var orders = await sx.GetOrders(maker: "other");
//var trades = await sx.GetTrades(bettor: "other", pageSize: 100, from: DateTime.UtcNow.AddDays(-1), to: DateTime.UtcNow);

var inactive = markets.data.markets.Where(m => m.status == "INACTIVE").ToArray();
var active = markets.data.markets.Where(m => m.status == "ACTIVE").ToArray();

var moneylineMarkets = markets.data.markets.Where(m => m.MarketType == MarketType.MoneylineIncludingOvertime).ToArray();
Market marketToBet = moneylineMarkets.OrderByDescending(m => m.StartTime).FirstOrDefault();

NewOrdersResponse result = await sx.PlaceOrders(new[]
{
    new PlaceOrder
    {
         Amount = new Amount(15m, "USDC"),
         Expiry = DateTime.UtcNow.AddHours(1),
         MarketHash = marketToBet.marketHash,
         Outcome1 = true,
         Price = new Price(PriceFormat.Decimal, 50m),
    },
});

CancelOrdersV2Response cancelResult = await sx.CancelOrdersV2(result.data.orders);

Console.ReadLine();
