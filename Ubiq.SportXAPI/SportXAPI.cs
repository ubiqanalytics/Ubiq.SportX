using IO.Ably;
using IO.Ably.Realtime;
using Jering.Javascript.NodeJS;
using Microsoft.Extensions.Logging;
using Nethereum.ABI;
using Nethereum.ABI.EIP712;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Numerics;
using System.Security.Cryptography;
using Ubiq.Definitions.Http;
using Ubiq.Definitions.Market;
using Ubiq.Extensions.Market;

namespace Ubiq.SportXAPI
{
    public class SportXAPI
    {
        private readonly ILogger<SportXAPI> m_Logger;
        private readonly IHttpClientHelper m_HttpClientHelper;
        private readonly HttpClient m_HttpClient;
        private readonly string m_WalletAddress;
        private readonly string m_ApiKey;
        private readonly string m_BaseUrl;
        private readonly string m_PrivateKey;

        private string m_ExecutorAddress;
        private string m_USDCBaseTokenAddress;
        private string m_ETHBaseTokenAddress;
        private string m_SXBaseTokenAddress;
        private Int32 m_ChainId;

        private static byte[] _Zero = new byte[] { 0 };
        private static decimal _OddsMultiplier = (decimal)Math.Pow(10, 20);
        private static decimal _USDCDivisor = 1000000m;
        private static decimal _ETHDivisor = 1000000000000000000m;
        private static decimal _SXDivisor = 1000000000000000000m;

        public const string _CancelAllSignatureFileLocation = "./node_signatures/dist/cancelAllSignature.js";
        public const string _CancelEventSignatureFileLocation = "./node_signatures/dist/cancelEventSignature.js";
        public const string _CancelSignatureFileLocation = "./node_signatures/dist/cancelSignature.js";
        public const string _CancelV2SignatureFileLocation = "./node_signatures/dist/cancelV2Signature.js";

        public event EventHandler WebSocketConnected;
        public event EventHandler WebSocketDisconnected;

        public event EventHandler<OrderUpdateMessage[]> OrdersUpdated;
        public event EventHandler<LineUpdatedMessage[]> LinesUpdated;
        public event EventHandler<MarketUpdateMessage[]> MarketsUpdated;
        public event EventHandler<TradeUpdateMessage> TradeUpdated;
        public event EventHandler<TradeUpdateMessage> MyTradeUpdated;

        public SportXAPI(ILogger<SportXAPI> logger, IHttpClientHelper httpClientExtensions, HttpClient httpClient, Uri baseUri, string privateKey, decimal makerCommissionRate, decimal takerCommissionRate, string apiKey)
        {
            m_Logger = logger;
            m_HttpClientHelper = httpClientExtensions;
            m_HttpClient = httpClient;
            m_BaseUrl = baseUri.ToString();
            m_WalletAddress = new EthECKey(privateKey).GetPublicAddress();
            m_ApiKey = apiKey;
            m_PrivateKey = privateKey;

            this.MakerCommissionRate = makerCommissionRate;
            this.TakerCommissionRate = takerCommissionRate;

            httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue();
            httpClient.DefaultRequestHeaders.CacheControl.MaxAge = TimeSpan.Zero;
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", m_ApiKey);
        }

        public string WalletAddress => m_WalletAddress;
        public decimal MakerCommissionRate { get; private set; }
        public decimal TakerCommissionRate { get; private set; }

        public async Task<MetadataResponse> Initialise(CancellationToken cancellation = default)
        {
            string metaUrl = $"{m_BaseUrl}metadata";
            MetadataResponse meta = await m_HttpClientHelper.GetAsync<MetadataResponse>(m_HttpClient, metaUrl.ToString(), requestName: "meta", cancellation: cancellation).ConfigureAwait(false);

            m_ExecutorAddress = meta.data.executorAddress;

            m_USDCBaseTokenAddress = meta.data.addresses.C416?.USDC;
            m_ETHBaseTokenAddress = meta.data.addresses.C416?.WETH;
            m_SXBaseTokenAddress = meta.data.addresses.C416?.WSX;

            if (meta.data.addresses.C416 is object)
            {
                m_ChainId = 416;
            }

            return meta;
        }

        public void InitialiseWebSocket()
        {
            var clientOptions = new ClientOptions
            {
                AuthUrl = new Uri(m_BaseUrl + "user/token"),
                AuthHeaders = new Dictionary<string, string> { { "x-api-key", m_ApiKey } },
            };
            var ably = new AblyRealtime(clientOptions);

            ably.Connection.On(state =>
            {
                if (state.Current == ConnectionState.Connected)
                {
                    // hook up subscribers when connection is made
                    _CreateChannel(ably, "markets", message => _ProcessMarketUpdateMessage(message));
                    _CreateChannel(ably, "main_line", message => _ProcessLineChangeMessage(message));
                    _CreateChannel(ably, "recent_trades", message => _ProcessTradeUpdateMessage(message));
                    _CreateChannel(ably, $"active_orders:{m_USDCBaseTokenAddress}:{m_WalletAddress}", message => _ProcessOrderMessage(message, m_USDCBaseTokenAddress));
                    _CreateChannel(ably, $"active_orders:{m_ETHBaseTokenAddress}:{m_WalletAddress}", message => _ProcessOrderMessage(message, m_ETHBaseTokenAddress));
                    _CreateChannel(ably, $"active_orders:{m_SXBaseTokenAddress}:{m_WalletAddress}", message => _ProcessOrderMessage(message, m_SXBaseTokenAddress));

                    WebSocketConnected?.Invoke(this, EventArgs.Empty);
                }
                else if (state.Current == ConnectionState.Disconnected || state.Current == ConnectionState.Failed)
                {
                    WebSocketDisconnected?.Invoke(this, EventArgs.Empty);
                }
            });
            ably.Connect();
        }

        private void _CreateChannel(AblyRealtime ably, string channelName, Action<Message> handler)
        {
            try
            {
                IRealtimeChannel channel = ably.Channels.Get(channelName);
                m_Logger.LogDebug($"{channelName} channel {channel?.State}");

                if (channel?.State == ChannelState.Initialized || channel?.State == ChannelState.Detached || channel?.State == ChannelState.Failed)
                {
                    channel.Subscribe(handler);
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error during _CreateChannel");
            }
        }

        private void _ProcessMarketUpdateMessage(Message message)
        {
            var marketUpdateData = message.Data as JArray;
            if (marketUpdateData is null)
            {
                return;
            }

            List<MarketUpdateMessage> marketUpdates = marketUpdateData.ToObject<List<MarketUpdateMessage>>();
            MarketsUpdated?.Invoke(this, marketUpdates.ToArray());
        }

        private void _ProcessLineChangeMessage(Message message)
        {
            var lineChangesData = message.Data as JArray;
            if (lineChangesData is null)
            {
                return;
            }

            List<LineUpdatedMessage> lineChanges = lineChangesData.ToObject<List<LineUpdatedMessage>>();
            LinesUpdated?.Invoke(this, lineChanges.ToArray());
        }

        private void _ProcessTradeUpdateMessage(Message message)
        {
            var tradeUpdateData = message.Data as JObject;
            if (tradeUpdateData is null)
            {
                return;
            }

            TradeUpdateMessage tradeUpdateMessage = tradeUpdateData.ToObject<TradeUpdateMessage>();

            if (tradeUpdateMessage.baseToken == m_USDCBaseTokenAddress)
            {
                tradeUpdateMessage.Stake = _ConvertCurrencyUSD(tradeUpdateMessage.stake);
            }
            else if (tradeUpdateMessage.baseToken == m_ETHBaseTokenAddress)
            {
                tradeUpdateMessage.Stake = _ConvertCurrencyETH(tradeUpdateMessage.stake);
            }
            else if (tradeUpdateMessage.baseToken == m_SXBaseTokenAddress)
            {
                tradeUpdateMessage.Stake = _ConvertCurrencySX(tradeUpdateMessage.stake);
            }
            else
            {
                return;
            }

            tradeUpdateMessage.Price = _OddsStringToPrice(tradeUpdateMessage.odds);

            // only set commission price for our bets
            tradeUpdateMessage.PriceWithCommission = tradeUpdateMessage.Price.RemoveCommission(tradeUpdateMessage.maker == true ? this.MakerCommissionRate : this.TakerCommissionRate);
            tradeUpdateMessage.CommissionRate = tradeUpdateMessage.maker == true ? this.MakerCommissionRate : this.TakerCommissionRate;

            if (tradeUpdateMessage.bettor == m_WalletAddress)
            {
                MyTradeUpdated?.Invoke(this, tradeUpdateMessage);
            }
            else
            {
                TradeUpdated?.Invoke(this, tradeUpdateMessage);
            }
        }

        private void _ProcessOrderMessage(Message message, string baseTokenAddress)
        {
            var orders = message.Data as JArray;
            if (orders is null)
            {
                return;
            }

            var updatesOrders = new List<OrderUpdateMessage>();
            foreach (JToken order in orders)
            {
                string fillAmountString = (string)order[3];
                string totalBetSizeString = (string)order[4];
                string percentageOddsString = (string)order[5];
                Int64? apiExpirySeconds = (Int64?)order[6];
                bool IsMakerBettingOutcomeOne = (bool)order[9];

                string currency = null;
                if (baseTokenAddress == m_USDCBaseTokenAddress)
                {
                    currency = "USDC";
                }
                else if (baseTokenAddress == m_ETHBaseTokenAddress)
                {
                    currency = "ETH";
                }
                else if (baseTokenAddress == m_SXBaseTokenAddress)
                {
                    currency = "SX";
                }

                Amount fillAmount = null;
                if (string.IsNullOrWhiteSpace(fillAmountString) == false)
                {
                    if (baseTokenAddress == m_USDCBaseTokenAddress)
                    {
                        fillAmount = _ConvertCurrencyUSD(fillAmountString);
                    }
                    else if (baseTokenAddress == m_ETHBaseTokenAddress)
                    {
                        fillAmount = _ConvertCurrencyETH(fillAmountString);
                    }
                    else if (baseTokenAddress == m_SXBaseTokenAddress)
                    {
                        fillAmount = _ConvertCurrencySX(fillAmountString);
                    }
                }

                Amount totalBetSize = null;
                if (string.IsNullOrWhiteSpace(totalBetSizeString) == false)
                {
                    if (baseTokenAddress == m_USDCBaseTokenAddress)
                    {
                        totalBetSize = _ConvertCurrencyUSD(totalBetSizeString);
                    }
                    else if (baseTokenAddress == m_ETHBaseTokenAddress)
                    {
                        totalBetSize = _ConvertCurrencyETH(totalBetSizeString);
                    }
                    else if (baseTokenAddress == m_SXBaseTokenAddress)
                    {
                        totalBetSize = _ConvertCurrencySX(totalBetSizeString);
                    }
                }

                Price price = _OddsStringToPrice(percentageOddsString);
                Price priceWithCommission = price.RemoveCommission(this.MakerCommissionRate);

                DateTime? apiExpiry = null;
                if (apiExpirySeconds != null)
                {
                    apiExpiry = DateTimeOffset.FromUnixTimeSeconds(apiExpirySeconds.Value).UtcDateTime;
                }

                var orderUpdateMessage = new OrderUpdateMessage
                {
                    OrderHash = (string)order[0],
                    MarketHash = (string)order[1],
                    Status = Enum.Parse<OrderStatus>((string)order[2]),
                    FillAmount = fillAmount,
                    TotalBetSize = totalBetSize,
                    Price = price,
                    PriceWithCommission = priceWithCommission,
                    ApiExpiry = apiExpiry,
                    Salt = (string)order[8],
                    IsMakerBettingOutcomeOne = IsMakerBettingOutcomeOne,
                    Signature = (string)order[10],
                    UpdateTime = BigInteger.Parse((string)order[11]),
                    Currency = currency,
                };
                updatesOrders.Add(orderUpdateMessage);
            }

            OrdersUpdated?.Invoke(this, updatesOrders.ToArray());
        }

        private Price _OddsStringToPrice(string percentageOddsString)
        {
            if (string.IsNullOrWhiteSpace(percentageOddsString) == true)
            {
                return null;
            }

            decimal percentageOddsBig = decimal.Parse(percentageOddsString);
            decimal percentageOdds = percentageOddsBig / _OddsMultiplier;
            return new Price(PriceFormat.Probability, percentageOdds).ToDecimalPrice();
        }

        public async Task<LeagueResponse> GetLeagues(CancellationToken cancellation = default)
        {
            string leaguesUrl = $"{m_BaseUrl}leagues";
            return await m_HttpClientHelper.GetAsync<LeagueResponse>(m_HttpClient, leaguesUrl, requestName: "leagues", cancellation: cancellation).ConfigureAwait(false);
        }

        public async Task<LeagueTeamsResponse> GetLeagueTeams(Int64 leagueId, CancellationToken cancellation = default)
        {
            string teamsUrl = $"{m_BaseUrl}leagues/teams/{leagueId}";
            return await m_HttpClientHelper.GetAsync<LeagueTeamsResponse>(m_HttpClient, teamsUrl, requestName: "teams", cancellation: cancellation).ConfigureAwait(false);
        }

        public async Task<MarketResponse> GetMarkets(Sport[] sports = null, CancellationToken cancellation = default)
        {
            Int32 pageSize = 40;

            string marketsUrl = $"{m_BaseUrl}markets/active?pageSize={pageSize}";

            if (sports?.Length > 0)
            {
                marketsUrl += $"&sportIds={string.Join(',', sports.Select(s => (Int32)s))}";
            }

            MarketResponse marketResponse = await m_HttpClientHelper.GetAsync<MarketResponse>(m_HttpClient, marketsUrl, requestName: "markets", cancellation: cancellation).ConfigureAwait(false);

            if (marketResponse?.data?.markets?.Length == pageSize)
            {
                var markets = new List<Market>(marketResponse.data.markets);

                string paginationKey = marketResponse.data.nextKey;
                while (true)
                {
                    MarketResponse marketResponseExtra = await m_HttpClientHelper.GetAsync<MarketResponse>(m_HttpClient, marketsUrl + $"&paginationKey={paginationKey}", requestName: "markets", cancellation: cancellation).ConfigureAwait(false);
                    paginationKey = marketResponseExtra?.data?.nextKey;

                    if (marketResponseExtra.data?.markets?.Length > 0)
                    {
                        markets.AddRange(marketResponseExtra.data.markets);
                    }

                    if (marketResponseExtra?.data?.markets is null || marketResponseExtra.data?.markets?.Length < pageSize)
                    {
                        break;
                    }
                }

                marketResponse.data.markets = markets.ToArray();
            }

            return marketResponse;
        }

        public async Task<FixtureResponse> GetFixtures(Int64 leagueId, CancellationToken cancellation = default)
        {
            string marketsUrl = $"{m_BaseUrl}fixture/active?leagueId={leagueId}";
            return await m_HttpClientHelper.GetAsync<FixtureResponse>(m_HttpClient, marketsUrl, requestName: $"fixtures_{leagueId}", cancellation: cancellation).ConfigureAwait(false);
        }

        public async Task<TradesResponse> GetTrades(string bettor = "self", bool? settled = null, string[] marketHashes = null, Int32 pageSize = 80, bool? maker = null, DateTime? from = null, DateTime? to = null, string token = null, DateTime? ourBetsSettlementCutoff = null, CancellationToken cancellation = default)
        {
            string baseTokenAddress = null;
            if (token == "USDC")
            {
                baseTokenAddress = m_USDCBaseTokenAddress;
            }
            else if (token == "ETH")
            {
                baseTokenAddress = m_ETHBaseTokenAddress;
            }
            else if (token == "SX")
            {
                baseTokenAddress = m_SXBaseTokenAddress;
            }

            string tradesUrl = $"{m_BaseUrl}trades";

            string bettorAddress = null;
            if (bettor == "self")
            {
                bettorAddress = m_WalletAddress;
            }
            else if (bettor != "other")
            {
                bettorAddress = bettor;
            }

            var tradesRequest = new TradesRequest
            {
                bettor = bettorAddress,
                settled = settled,
                baseToken = baseTokenAddress,
                marketHashes = marketHashes,
                pageSize = pageSize,
                maker = maker,
                startDate = from == null ? null : new DateTimeOffset(from.Value).ToUnixTimeSeconds(),
                endDate = to == null ? null : new DateTimeOffset(to.Value).ToUnixTimeSeconds(),
            };

            string settledName = settled.HasValue == true ? settled.Value == true ? "Settled" : "Unsettled" : "Both";

            TradesResponse tradesResponse = await m_HttpClientHelper.GetAsync<TradesResponse>(m_HttpClient, tradesUrl + tradesRequest.GetParams(), requestName: $"trades_{bettor}_{settledName}", cancellation: cancellation).ConfigureAwait(false);

            if (tradesResponse?.data?.count > tradesResponse?.data.trades?.Length)
            {
                // more items than page size returned
                tradesRequest.paginationKey = tradesResponse.data.nextKey;

                var allTrades = new List<Trade>(tradesResponse.data.trades);
                while (true)
                {
                    TradesResponse pageResponse = await m_HttpClientHelper.GetAsync<TradesResponse>(m_HttpClient, tradesUrl + tradesRequest.GetParams(), requestName: $"trades_{bettor}_{settledName}", cancellation: cancellation).ConfigureAwait(false);
                    if (pageResponse?.data?.trades?.Length > 0)
                    {
                        allTrades.AddRange(pageResponse.data.trades);
                    }

                    if (allTrades.Count >= tradesResponse.data.count || pageResponse?.data?.trades is null || pageResponse?.data?.trades.Length == 0 || pageResponse?.data?.nextKey is null)
                    {
                        break;
                    }

                    tradesRequest.paginationKey = pageResponse.data.nextKey;
                }

                tradesResponse.data.trades = allTrades.ToArray();
            }

            if (tradesResponse?.data?.trades?.Length > 0)
            {
                // remove our own trades if requesting others
                if (bettor == "other")
                {
                    tradesResponse.data.trades = tradesResponse.data.trades.Where(t => t.bettor != m_WalletAddress).ToArray();
                }

                foreach (Trade trade in tradesResponse.data.trades)
                {
                    if (trade.bettor == m_WalletAddress)
                    {
                        // ignore bets that have settled more recently than settlement cutoff
                        if (ourBetsSettlementCutoff != null && trade.settleDate != null)
                        {
                            if (trade.settleDate.Value > ourBetsSettlementCutoff.Value)
                            {
                                continue;
                            }
                        }
                    }

                    if (trade.baseToken == m_USDCBaseTokenAddress)
                    {
                        trade.Stake = _ConvertCurrencyUSD(trade.stake);
                    }
                    else if (trade.baseToken == m_ETHBaseTokenAddress)
                    {
                        trade.Stake = _ConvertCurrencyETH(trade.stake);
                    }
                    else if (trade.baseToken == m_SXBaseTokenAddress)
                    {
                        trade.Stake = _ConvertCurrencySX(trade.stake);
                    }
                    else
                    {
                        continue;
                    }

                    trade.Price = _OddsStringToPrice(trade.odds);
                    trade.PriceWithCommission = trade.Price.RemoveCommission(trade.maker == true ? this.MakerCommissionRate : this.TakerCommissionRate);
                    trade.CommissionRate = trade.maker == true ? this.MakerCommissionRate : this.TakerCommissionRate;
                }

                tradesResponse.data.trades = tradesResponse.data.trades.Where(t => t.Stake is object).ToArray();
            }

            return tradesResponse;
        }

        public async Task<OrdersResponse> GetOrders(IEnumerable<string> marketHashes = null, string token = null, string maker = "self", CancellationToken cancellation = default)
        {
            string makerAddress = null;
            if (maker == "self")
            {
                makerAddress = m_WalletAddress;
            }
            else if (maker != "other")
            {
                makerAddress = maker;
            }

            string baseTokenAddress = null;
            if (token == "USDC")
            {
                baseTokenAddress = m_USDCBaseTokenAddress;
            }
            else if (token == "ETH")
            {
                baseTokenAddress = m_ETHBaseTokenAddress;
            }
            else if (token == "SX")
            {
                baseTokenAddress = m_SXBaseTokenAddress;
            }

            string ordersUrl = $"{m_BaseUrl}orders";
            var ordersRequest = new OrdersRequest
            {
                baseToken = baseTokenAddress,
                maker = makerAddress,
                marketHashes = marketHashes == null ? null : marketHashes.ToArray(),
            };

            OrdersResponse response = await m_HttpClientHelper.GetAsync<OrdersResponse>(m_HttpClient, ordersUrl + ordersRequest.GetParams(), requestName: "orders", cancellation: cancellation).ConfigureAwait(false);

            // remove our own trades if requesting others
            if (maker == "other")
            {
                response.data = response.data.Where(t => t.maker != m_WalletAddress).ToArray();
            }

            return response;
        }

        public async Task<CancelAllOrdersResponse> CancelAllOrders(CancellationToken cancellation = default)
        {
            string cancelOrdersUrl = $"{m_BaseUrl}orders/cancel/all";
            var cancelOrdersRequest = new CancelAllOrdersRequest
            {
                maker = m_WalletAddress,
                salt = new BigInteger(RandomNumberGenerator.GetBytes(32).Concat(_Zero).ToArray()).ToString(),
                timestamp = DateTimeOffset.UtcNow.AddSeconds(1).ToUnixTimeSeconds(),
            };

            cancelOrdersRequest.signature = await StaticNodeJSService.InvokeFromFileAsync<string>(_CancelAllSignatureFileLocation, args: new object[] { cancelOrdersRequest.salt, cancelOrdersRequest.timestamp, m_PrivateKey, m_ChainId });

            return await m_HttpClientHelper.PostAsync<CancelAllOrdersResponse, CancelAllOrdersRequest>(m_HttpClient, cancelOrdersUrl, cancelOrdersRequest, requestName: "ordersCancelAll", cancellation: cancellation).ConfigureAwait(false);
        }

        public async Task<CancelOrdersV2Response> CancelEventOrders(string sportXEventId, CancellationToken cancellation = default)
        {
            string cancelOrdersUrl = $"{m_BaseUrl}orders/cancel/event";
            var cancelOrdersRequest = new CancelEventOrdersRequest
            {
                sportXeventId = sportXEventId,
                maker = m_WalletAddress,
                salt = new BigInteger(RandomNumberGenerator.GetBytes(32).Concat(_Zero).ToArray()).ToString(),
                timestamp = DateTimeOffset.UtcNow.AddSeconds(1).ToUnixTimeSeconds(),
            };

            cancelOrdersRequest.signature = await StaticNodeJSService.InvokeFromFileAsync<string>(_CancelEventSignatureFileLocation, args: new object[] { cancelOrdersRequest.sportXeventId, cancelOrdersRequest.salt, cancelOrdersRequest.timestamp, m_PrivateKey, m_ChainId });

            return await m_HttpClientHelper.PostAsync<CancelOrdersV2Response, CancelEventOrdersRequest>(m_HttpClient, cancelOrdersUrl, cancelOrdersRequest, requestName: "ordersCancelEvent", cancellation: cancellation).ConfigureAwait(false);
        }

        public async Task<CancelOrdersV2Response> CancelOrdersV2(IEnumerable<string> orderHashes, CancellationToken cancellation = default)
        {
            if (orderHashes == null || orderHashes.Count() == 0)
            {
                return CancelOrdersV2Response.Success();
            }

            byte[] salt = RandomNumberGenerator.GetBytes(32).Concat(_Zero).ToArray();

            string cancelOrdersUrl = $"{m_BaseUrl}orders/cancel/v2";
            var cancelOrdersRequest = new CancelOrdersV2Request
            {
                orderHashes = orderHashes.ToArray(),
                maker = m_WalletAddress,
                salt = new BigInteger(salt).ToString(),
                timestamp = DateTimeOffset.UtcNow.AddSeconds(1).ToUnixTimeSeconds(),
            };

            // new version, doesnt work yet
            string sig = _SignV2Cancellation(cancelOrdersRequest.orderHashes, cancelOrdersRequest.timestamp, salt);

            cancelOrdersRequest.signature = await StaticNodeJSService.InvokeFromFileAsync<string>(_CancelV2SignatureFileLocation, args: new object[] { cancelOrdersRequest.orderHashes, cancelOrdersRequest.salt, cancelOrdersRequest.timestamp, m_PrivateKey, m_ChainId });

            return await m_HttpClientHelper.PostAsync<CancelOrdersV2Response, CancelOrdersV2Request>(m_HttpClient, cancelOrdersUrl, cancelOrdersRequest, requestName: "ordersCancel", cancellation: cancellation).ConfigureAwait(false);
        }

        public async Task<CancelOrdersResponse> CancelOrders(IEnumerable<string> orderHashes, CancellationToken cancellation = default)
        {
            if (orderHashes == null || orderHashes.Count() == 0)
            {
                return CancelOrdersResponse.Success();
            }

            string cancelOrdersUrl = $"{m_BaseUrl}orders/cancel";
            var cancelOrdersRequest = new CancelOrdersRequest
            {
                orders = orderHashes.ToArray(),
                message = "Are you sure you want to cancel these orders",
            };

            cancelOrdersRequest.cancelSignature = await StaticNodeJSService.InvokeFromFileAsync<string>(_CancelSignatureFileLocation, args: new object[] { cancelOrdersRequest.orders, m_PrivateKey, m_ChainId });

            return await m_HttpClientHelper.PostAsync<CancelOrdersResponse, CancelOrdersRequest>(m_HttpClient, cancelOrdersUrl, cancelOrdersRequest, requestName: "ordersCancel", cancellation: cancellation).ConfigureAwait(false);
        }

        private SignedNewOrder _CreateOrder(PlaceOrder order, string baseTokenAddress)
        {
            // rounding probability to lower 0.25%
            decimal probability = order.Price.ToProbability();
            decimal probabilityRounded = probability.RoundProbabilityDownToQuarter();
            decimal percentageOdds = _OddsMultiplier * probabilityRounded;
            string percentageOddsString = percentageOdds.ToString("F0");

            var signedOrder = new SignedNewOrder
            {
                marketHash = order.MarketHash,
                percentageOdds = percentageOddsString,
                maker = m_WalletAddress,
                salt = new BigInteger(RandomNumberGenerator.GetBytes(32).Concat(_Zero).ToArray()).ToString(),
                executor = m_ExecutorAddress,
                baseToken = baseTokenAddress,
                expiry = 2209006800,
                apiExpiry = new DateTimeOffset(order.Expiry).ToUnixTimeSeconds(),
                isMakerBettingOutcomeOne = order.Outcome1,
            };

            return signedOrder;
        }

        public async Task<NewOrdersResponse> PlaceOrders(IEnumerable<PlaceOrder> orders, CancellationToken cancellation = default)
        {
            var signedNewOrders = new List<SignedNewOrder>();
            foreach (PlaceOrder order in orders)
            {
                string betSize;
                string baseTokenAddress;
                if (order.Amount.Currency == "USDC")
                {
                    var bigInt = new BigInteger(order.Amount.Value * _USDCDivisor);
                    betSize = bigInt.ToString();
                    baseTokenAddress = m_USDCBaseTokenAddress;
                }
                else if (order.Amount.Currency == "ETH")
                {
                    var bigInt = new BigInteger(order.Amount.Value * _ETHDivisor);
                    betSize = bigInt.ToString();
                    baseTokenAddress = m_ETHBaseTokenAddress;
                }
                else if (order.Amount.Currency == "SX")
                {
                    var bigInt = new BigInteger(order.Amount.Value * _SXDivisor);
                    betSize = bigInt.ToString();
                    baseTokenAddress = m_SXBaseTokenAddress;
                }
                else
                {
                    throw new InvalidOperationException($"Invalid currency {order.Amount.Currency}");
                }

                SignedNewOrder signedNewOrder = _CreateOrder(order, baseTokenAddress);
                signedNewOrder.totalBetSize = betSize;
                signedNewOrders.Add(signedNewOrder);
            }

            return await _PlaceOrders(signedNewOrders, cancellation);
        }

        public async Task<NewOrdersResponse> _PlaceOrders(IEnumerable<SignedNewOrder> signedNewOrders, CancellationToken cancellation = default)
        {
            string ordersUrl = $"{m_BaseUrl}orders/new";

            var ordersRequest = new NewOrdersRequest
            {
                orders = signedNewOrders.ToArray(),
            };

            _SignOrder(ordersRequest);

            return await m_HttpClientHelper.PostAsync<NewOrdersResponse, NewOrdersRequest>(m_HttpClient, ordersUrl, ordersRequest, requestName: "ordersCreate", cancellation: cancellation).ConfigureAwait(false);
        }

        private void _SignOrder(NewOrdersRequest newOrdersRequest)
        {
            var signer = new EthereumMessageSigner();
            var abiEncode = new ABIEncode();

            foreach (SignedNewOrder order in newOrdersRequest.orders)
            {
                byte[] orderHash = abiEncode.GetSha3ABIEncodedPacked(
                    new ABIValue("bytes32", order.marketHash.HexToByteArray()),
                    new ABIValue("address", order.baseToken),
                    new ABIValue("uint256", BigInteger.Parse(order.totalBetSize)),
                    new ABIValue("uint256", BigInteger.Parse(order.percentageOdds)),
                    new ABIValue("uint256", BigInteger.Parse("2209006800")),
                    new ABIValue("uint256", BigInteger.Parse(order.salt)),
                    new ABIValue("address", order.maker),
                    new ABIValue("address", order.executor),
                    new ABIValue("bool", order.isMakerBettingOutcomeOne));

                order.signature = signer.Sign(orderHash, m_PrivateKey);
            }
        }

        private Amount _ConvertCurrencyUSD(string amountString)
        {
            decimal amountDecimal = decimal.Parse(amountString);
            if (amountDecimal == 0)
            {
                return new Amount(0, "USDC");
            }
            else
            {
                // if not 0, round up to something reasonable
                decimal amount = Math.Max(amountDecimal / _USDCDivisor, 0.001m);
                return new Amount(amount, "USDC");
            }
        }

        private Amount _ConvertCurrencyETH(string amountString)
        {
            decimal amountDecimal = decimal.Parse(amountString);
            if (amountDecimal == 0)
            {
                return new Amount(0, "ETH");
            }
            else
            {
                // if not 0, round up to something reasonable
                decimal amount = Math.Max(amountDecimal / _ETHDivisor, 0.000001m);
                return new Amount(amount, "ETH");
            }
        }

        private Amount _ConvertCurrencySX(string amountString)
        {
            decimal amountDecimal = decimal.Parse(amountString);
            if (amountDecimal == 0)
            {
                return new Amount(0, "SX");
            }
            else
            {
                // if not 0, round up to something reasonable
                decimal amount = Math.Max(amountDecimal / _SXDivisor, 0.000001m);
                return new Amount(amount, "SX");
            }
        }

        public class CancelOrderV2SportX
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public Int32 ChainId { get; set; }
            public byte[] Salt { get; set; }
        }

        private string _SignV2Cancellation(string[] orderHashes, Int64 timestamp, byte[] salt)
        {
            var cancelOrder = new CancelOrderV2SportX()
            {
                Name = "CancelOrderV2SportX",
                Version = "1.0",
                ChainId = m_ChainId,
                Salt = salt,
            };

            var typedData = new TypedData<CancelOrderV2SportX>
            {
                Domain = cancelOrder,
                Types = new Dictionary<string, MemberDescription[]>
                {
                    ["EIP712Domain"] = new[]
                    {
                        new MemberDescription {Name = "name", Type = "string"},
                        new MemberDescription {Name = "version", Type = "string"},
                        new MemberDescription {Name = "chainId", Type = "uint256"},
                        new MemberDescription {Name = "salt", Type = "bytes32"},
                    },
                    ["Details"] = new[]
                    {
                        new MemberDescription {Name = "orderHashes", Type = "string[]"},
                        new MemberDescription {Name = "timestamp", Type = "uint256"},
                    },
                },
                PrimaryType = "Details",
                Message = new[]
                {
                    new MemberValue
                    {
                        TypeName = "Details", Value = new[]
                        {
                            new MemberValue { TypeName = "string[]", Value = orderHashes },
                            new MemberValue { TypeName = "uint256", Value = new BigInteger(timestamp) },
                        }
                    },
                }
            };

            return Eip712TypedDataSigner.Current.SignTypedData(typedData, new EthECKey(m_PrivateKey));
        }
    }
}
