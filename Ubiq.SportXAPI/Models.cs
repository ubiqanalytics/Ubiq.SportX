using Newtonsoft.Json;
using System.Numerics;
using System.Text;
using Ubiq.Definitions.Market;

namespace Ubiq.SportXAPI
{
    public enum Sport
    {
        Basketball = 1,
        Hockey = 2,
        Baseball = 3,
        Golf = 4,
        Soccer = 5,
        Tennis = 6,
        MixedMartialArts = 7,
        Football = 8,
        ESports = 9,
        Custom = 10,
        RugbyUnion = 11,
        Racing = 12,
        Boxing = 13,
        Crypto = 14,
        Cricket = 15,
        Economics = 16,
        Politics = 17,
        Entertainment = 18,
        Medicinal = 19,
        RugbyLeague = 20,
        Olympics = 21,
        Athletics = 22,
    }

    public enum MarketType
    {
        OneXTwo = 1,
        Moneyline = 52,
        MoneylineIncludingOvertime = 226,
        MoneylineFirstHalf = 63,
        MoneylineFirstPeriod = 202,
        MoneylineSecondPeriod = 203,
        MoneylineThirdPeriod = 204,
        MoneylineFourthPeriod = 205,
        MoneylineFirstFiveInnings = 1618,
        AsianHandicap = 3,
        AsianHandicapIncludingOvertime = 342,
        AsianHandicapFirstHalf = 53,
        AsianHandicapGames = 201,
        AsianHandicapFirstPeriod = 64,
        AsianHandicapSecondPeriod = 65,
        AsianHandicapThirdPeriod = 66,
        AsianHandicapFirstFiveInnings = 281,
        AsianHandicapSets = 866,
        OverUnder = 2,
        OverUnderIncludingOvertime = 28,
        OverUnderFirstHalf = 77,
        OverUnderAsian = 835,
        OverUnderGames = 166,
        OverUnderRounds = 29,
        OverUnderMaps = 1536,
        OverUnderSets = 165,
        OverUnderFirstPeriod = 21,
        OverUnderSecondPeriod = 45,
        OverUnderThirdPeriod = 46,
        OverUnderFirstFiveInnings = 236,
        ToQualify = 88,
        OutrightWinner = 274,
    }

    public class MetadataResponse
    {
        public string status { get; set; }
        public Metadata data { get; set; }
    }

    public class Metadata
    {
        public string executorAddress { get; set; }
        public Oraclefees oracleFees { get; set; }
        public Sportxaffiliate sportXAffiliate { get; set; }
        public Makerorderminimums makerOrderMinimums { get; set; }
        public Takerminimums takerMinimums { get; set; }
        public Addresses addresses { get; set; }
        public bool bettingEnabled { get; set; }
        public decimal totalVolume { get; set; }
        public string domainVersion { get; set; }
        public string EIP712FillHasher { get; set; }
        public string TokenTransferProxy { get; set; }
        public int bridgeFee { get; set; }
    }

    public class Oraclefees
    {
        public string _0xA173954Cc4b1810C0dBdb007522ADbC182DaB380 { get; set; }
        public string _0xe2aa35C2039Bd0Ff196A6Ef99523CC0D3972ae3e { get; set; }
        public string _0xaa99bE3356a11eE92c3f099BD7a038399633566f { get; set; }
    }

    public class Sportxaffiliate
    {
        public string address { get; set; }
        public string amount { get; set; }
    }

    public class Makerorderminimums
    {
        public string _0xA173954Cc4b1810C0dBdb007522ADbC182DaB380 { get; set; }
        public string _0xe2aa35C2039Bd0Ff196A6Ef99523CC0D3972ae3e { get; set; }
        public string _0xaa99bE3356a11eE92c3f099BD7a038399633566f { get; set; }
    }

    public class Takerminimums
    {
        public string _0xA173954Cc4b1810C0dBdb007522ADbC182DaB380 { get; set; }
        public string _0xe2aa35C2039Bd0Ff196A6Ef99523CC0D3972ae3e { get; set; }
        public string _0xaa99bE3356a11eE92c3f099BD7a038399633566f { get; set; }
    }

    public class Addresses
    {
        [JsonProperty("416")]
        public C416 C416 { get; set; }
    }

    public class C416
    {
        public string WETH { get; set; }
        public string USDC { get; set; }
        public string WSX { get; set; }
    }

    public class LeagueResponse
    {
        public string status { get; set; }
        public League[] data { get; set; }
    }

    public class League
    {
        public Int32 leagueId { get; set; }
        public string label { get; set; }
        public Int32 sportId { get; set; }
        public bool active { get; set; }
        public bool homeTeamFirst { get; set; }

        public void Trim()
        {
            this.label = this.label?.Trim();
        }

        public Sport Sport
        {
            get
            {
                return (Sport)sportId;
            }
        }

        public override string ToString()
        {
            return $"{leagueId} {label}";
        }
    }

    public class LeagueTeamsResponse
    {
        public string status { get; set; }
        public LeagueTeams data { get; set; }
    }

    public class LeagueTeams
    {
        public Team[] teams { get; set; }
    }

    public class Team
    {
        public Int64 id { get; set; }
        public string name { get; set; }

        public void Trim()
        {
            this.name = this.name?.Trim();
        }

        public override string ToString()
        {
            return $"{id} {name}";
        }
    }

    public class TradesRequest
    {
        public Int64? startDate { get; set; }
        public Int64? endDate { get; set; }
        public string[] marketHashes { get; set; }
        public string baseToken { get; set; }
        public string bettor { get; set; }
        public bool? maker { get; set; }
        public bool? settled { get; set; }
        public Int32? pageSize { get; set; }
        public string paginationKey { get; set; }
        public string tradeStatus { get; set; }

        public string GetParams()
        {
            var sb = new StringBuilder($"?rand={Guid.NewGuid().GetHashCode()}");
            if (startDate != null)
            {
                sb.Append($"&startDate={startDate}");
            }
            if (endDate != null)
            {
                sb.Append($"&endDate={endDate}");
            }
            if (marketHashes != null)
            {
                sb.Append($"&marketHashes={string.Join(',', marketHashes ?? Array.Empty<string>())}");
            }
            if (baseToken != null)
            {
                sb.Append($"&baseToken={baseToken}");
            }
            if (tradeStatus != null)
            {
                sb.Append($"&tradeStatus={tradeStatus}");
            }
            if (bettor != null)
            {
                sb.Append($"&bettor={bettor}");
            }
            if (maker != null)
            {
                sb.Append($"&maker={maker.ToString().ToLower()}");
            }
            if (settled != null)
            {
                sb.Append($"&settled={settled.ToString().ToLower()}");
            }
            if (pageSize != null)
            {
                sb.Append($"&pageSize={pageSize}");
            }
            if (paginationKey != null)
            {
                sb.Append($"&paginationKey={paginationKey}");
            }

            return sb.ToString();
        }
    }

    public class TradesResponse
    {
        public string status { get; set; }
        public TradeData data { get; set; }
    }

    public class TradeData
    {
        public Trade[] trades { get; set; }
        public string nextKey { get; set; }
        public Int32 pageSize { get; set; }
        public Int32 count { get; set; }
    }

    public class Trade
    {
        [JsonConstructor]
        public Trade()
        {
        }

        public Trade(TradeUpdateMessage tradeUpdate)
        {
            this.bettor = tradeUpdate.bettor;
            this.baseToken = tradeUpdate.baseToken;
            this.betTime = tradeUpdate.betTime;
            this.betTimeValue = tradeUpdate.betTimeValue;
            this.bettingOutcomeOne = tradeUpdate.bettingOutcomeOne;
            this.fillHash = tradeUpdate.fillHash;
            this.id = tradeUpdate.id;
            this.maker = tradeUpdate.maker;
            this.marketHash = tradeUpdate.marketHash;
            this.odds = tradeUpdate.odds;
            this.orderHash = tradeUpdate.orderHash;
            this.settled = tradeUpdate.settled;
            this.stake = tradeUpdate.stake;
            this.tradeStatus = tradeUpdate.status;
            this.Price = tradeUpdate.Price;
            this.PriceWithCommission = tradeUpdate.PriceWithCommission;
            this.CommissionRate = tradeUpdate.CommissionRate;
            this.Stake = tradeUpdate.Stake;
        }

        [JsonProperty("_id")]
        public string id { get; set; }
        public string baseToken { get; set; }
        public string bettor { get; set; }
        public string stake { get; set; }
        public string odds { get; set; }
        public string orderHash { get; set; }
        public string marketHash { get; set; }
        public decimal betTimeValue { get; set; }
        public bool maker { get; set; }
        public Int64 betTime { get; set; }
        public bool settled { get; set; }
        public decimal settleValue { get; set; }
        public bool bettingOutcomeOne { get; set; }
        public string fillHash { get; set; }
        public string tradeStatus { get; set; }
        public bool valid { get; set; }
        public string contractsVersion { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public string fillOrderHash { get; set; }
        public Int16? outcome { get; set; }
        public DateTime? settleDate { get; set; }
        public object settleTxHash { get; set; }

        public Price Price { get; set; }
        public Price PriceWithCommission { get; set; }
        public decimal? CommissionRate { get; set; }
        public Amount Stake { get; set; }

        public bool Voided
        {
            get
            {
                return this.tradeStatus.ToUpper().Contains("FAIL");
            }
        }

        public DateTime BetDateTime
        {
            get
            {
                return DateTimeOffset.FromUnixTimeSeconds(this.betTime).UtcDateTime;
            }
        }
    }

    public class PlaceOrder
    {
        public string MarketHash { get; set; }
        public Price Price { get; set; }
        public bool Outcome1 { get; set; }
        public DateTime Expiry { get; set; }
        public Amount Amount { get; set; }

        public override string ToString()
        {
            return $"({this.MarketHash}.{this.Outcome1}, {this.Price}, {this.Amount})";
        }
    }

    public class OrdersRequest
    {
        public string[] marketHashes { get; set; }
        public string baseToken { get; set; }
        public string maker { get; set; }

        public string GetParams()
        {
            var sb = new StringBuilder($"?rand={Guid.NewGuid().GetHashCode()}");
            if (marketHashes != null)
            {
                sb.Append($"&marketHashes={string.Join(',', marketHashes ?? Array.Empty<string>())}");
            }

            if (baseToken != null)
            {
                sb.Append($"&baseToken={baseToken}");
            }

            if (maker != null)
            {
                sb.Append($"&maker={maker}");
            }

            return sb.ToString();
        }
    }

    public class OrdersResponse
    {
        public string status { get; set; }
        public Order[] data { get; set; }
    }

    public class Order
    {
        public string marketHash { get; set; }
        public string fillAmount { get; set; }
        public string orderHash { get; set; }
        public string maker { get; set; }
        public string totalBetSize { get; set; }
        public string percentageOdds { get; set; }
        public string baseToken { get; set; }
        public string executor { get; set; }
        public string salt { get; set; }
        public bool isMakerBettingOutcomeOne { get; set; }
        public string signature { get; set; }
        public Int64 expiry { get; set; }
        public Int64 apiExpiry { get; set; }
    }

    public class MarketResponse
    {
        public string status { get; set; }
        public Markets data { get; set; }
    }

    public class Markets
    {
        public Market[] markets { get; set; }
        public string nextKey { get; set; }
    }

    public class Market
    {
        [JsonConstructor]
        public Market()
        {
        }

        public Market(MarketUpdateMessage marketUpdate)
        {
            gameTime = marketUpdate.gameTime;
            group1 = marketUpdate.group1;
            group2 = marketUpdate.group2;
            homeTeamFirst = marketUpdate.homeTeamFirst;
            leagueId = marketUpdate.leagueId;
            leagueLabel = marketUpdate.leagueLabel;
            line = marketUpdate.line;
            liveEnabled = marketUpdate.liveEnabled;
            marketHash = marketUpdate.marketHash;
            participantOneId = marketUpdate.participantOneId;
            participantTwoId = marketUpdate.participantTwoId;
            outcomeOneName = marketUpdate.outcomeOneName;
            outcomeTwoName = marketUpdate.outcomeTwoName;
            outcomeVoidName = marketUpdate.outcomeVoidName;
            sportId = marketUpdate.sportId;
            sportLabel = marketUpdate.sportLabel;
            sportXeventId = marketUpdate.sportXeventId;
            status = marketUpdate.status;
            teamOneName = marketUpdate.teamOneName;
            teamTwoName = marketUpdate.teamTwoName;
            type = marketUpdate.type;
        }

        public string status { get; set; }
        public string marketHash { get; set; }
        public string outcomeOneName { get; set; }
        public string outcomeTwoName { get; set; }
        public string outcomeVoidName { get; set; }
        public string teamOneName { get; set; }
        public string teamTwoName { get; set; }
        public Int32 participantOneId { get; set; }
        public Int32 participantTwoId { get; set; }
        public Int32 type { get; set; }
        public Int64 gameTime { get; set; }
        public decimal line { get; set; }
        public string sportXeventId { get; set; }
        public bool liveEnabled { get; set; }
        public string sportLabel { get; set; }
        public Int32 sportId { get; set; }
        public Int32 leagueId { get; set; }
        public bool homeTeamFirst { get; set; }
        public string leagueLabel { get; set; }
        public bool mainLine { get; set; }
        public string group1 { get; set; }
        public string group2 { get; set; }
        public Int32 teamOneScore { get; set; }
        public Int32 teamTwoScore { get; set; }
        public string marketMeta { get; set; }

        public MarketType MarketType
        {
            get
            {
                return (MarketType)this.type;
            }
        }

        public Sport Sport
        {
            get
            {
                return (Sport)sportId;
            }
        }

        public DateTime StartTime
        {
            get
            {
                return DateTimeOffset.FromUnixTimeSeconds(this.gameTime).UtcDateTime;
            }
        }

        public void Trim()
        {
            this.status = this.status?.Trim();
            this.marketHash = this.marketHash?.Trim();
            this.outcomeOneName = this.outcomeOneName?.Trim();
            this.outcomeTwoName = this.outcomeTwoName?.Trim();
            this.outcomeVoidName = this.outcomeVoidName?.Trim();
            this.teamOneName = this.teamOneName?.Trim();
            this.teamTwoName = this.teamTwoName?.Trim();
            this.sportXeventId = this.sportXeventId?.Trim();
            this.sportLabel = this.sportLabel?.Trim();
            this.leagueLabel = this.leagueLabel?.Trim();
            this.group1 = this.group1?.Trim();
            this.group2 = this.group2?.Trim();
            this.marketMeta = this.marketMeta?.Trim();
        }

        public override string ToString()
        {
            return $"{this.MarketType}, {this.sportLabel}, {this.leagueLabel}, {this.outcomeOneName}, {this.outcomeTwoName}";
        }
    }

    public class FixtureResponse
    {
        public string status { get; set; }
        public Fixture[] data { get; set; }
    }

    public class Fixture
    {
        public int participantOneId { get; set; }
        public string participantOneName { get; set; }
        public int participantTwoId { get; set; }
        public string participantTwoName { get; set; }
        public DateTime startDate { get; set; }
        public Int32 status { get; set; }
        public Int32 leagueId { get; set; }
        public string leagueLabel { get; set; }
        public Int32 sportId { get; set; }
        public string eventId { get; set; }
        public Participant[] participants { get; set; }

        public Sport Sport
        {
            get
            {
                return (Sport)sportId;
            }
        }

        public void Trim()
        {
            this.participantOneName = this.participantOneName?.Trim();
            this.participantTwoName = this.participantTwoName?.Trim();
            this.leagueLabel = this.leagueLabel?.Trim();
            this.eventId = this.eventId?.Trim();
        }

        public override string ToString()
        {
            return $"{Sport} - {participantOneName} vs {participantTwoName} in {leagueLabel}";
        }
    }

    public class Participant
    {
        public Int64 id { get; set; }
        public string name { get; set; }

        public void Trim()
        {
            this.name = this.name?.Trim();
        }

        public override string ToString()
        {
            return name;
        }
    }

    public class NewOrdersRequest
    {
        public SignedNewOrder[] orders { get; set; }
    }

    public class SignedNewOrder
    {
        public string marketHash { get; set; }
        public string maker { get; set; }
        public string totalBetSize { get; set; }
        public string percentageOdds { get; set; }
        public string baseToken { get; set; }
        public Int64 apiExpiry { get; set; }
        public Int64 expiry { get; set; }
        public string executor { get; set; }
        public bool isMakerBettingOutcomeOne { get; set; }
        public string signature { get; set; }
        public string salt { get; set; }
    }

    public class NewOrdersResponse
    {
        public string status { get; set; }
        public OrderData data { get; set; }
    }

    public class OrderData
    {
        public string[] orders { get; set; }
    }

    public class CancelAllOrdersRequest
    {
        public string maker { get; set; }
        public string salt { get; set; }
        public string signature { get; set; }
        public Int64 timestamp { get; set; }
    }

    public class CancelAllOrdersResponse
    {
        public string status { get; set; }
        public CancelAllData data { get; set; }
    }

    public class CancelAllData
    {
        public Int32 cancelledCount { get; set; }
    }

    public class CancelEventOrdersRequest
    {
        public string sportXeventId { get; set; }
        public string maker { get; set; }
        public string salt { get; set; }
        public string signature { get; set; }
        public Int64 timestamp { get; set; }
    }

    public class CancelOrdersV2Request
    {
        public string[] orderHashes { get; set; }
        public string signature { get; set; }
        public string salt { get; set; }
        public string maker { get; set; }
        public Int64 timestamp { get; set; }
    }

    public class CancelOrdersV2Response
    {
        public string status { get; set; }
        public CancelV2Data data { get; set; }

        public static CancelOrdersV2Response Success()
        {
            return new CancelOrdersV2Response()
            {
                status = "success",
                data = new CancelV2Data()
                {
                    cancelledCount = 0,
                }
            };
        }
    }

    public class CancelV2Data
    {
        public Int32 cancelledCount { get; set; }
    }

    public class CancelOrdersRequest
    {
        public string[] orders { get; set; }
        public string message { get; set; }
        public string cancelSignature { get; set; }
    }

    public class CancelOrdersResponse
    {
        public string status { get; set; }
        public CancelData data { get; set; }

        public static CancelOrdersResponse Success()
        {
            return new CancelOrdersResponse()
            {
                data = new CancelData { orderHashes = new string[0] },
                status = "success",
            };
        }
    }

    public class CancelData
    {
        public string[] orderHashes { get; set; }
    }

    public enum OrderStatus
    {
        ACTIVE,
        INACTIVE,
    }

    public class OrderUpdateMessage
    {
        public string OrderHash { get; set; }
        public string MarketHash { get; set; }
        public OrderStatus Status { get; set; }
        public Amount FillAmount { get; set; }
        public Amount TotalBetSize { get; set; }
        public Price Price { get; set; }
        public Price PriceWithCommission { get; set; }
        public DateTime? ApiExpiry { get; set; }
        public string Salt { get; set; }
        public bool IsMakerBettingOutcomeOne { get; set; }
        public string Signature { get; set; }
        public BigInteger UpdateTime { get; set; }
        public string Currency { get; set; }
    }

    public class LineUpdatedMessage
    {
        public string sportXeventId { get; set; }
        public Int32 marketType { get; set; }
        public string marketHash { get; set; }
        public string updateTime { get; set; }

        public MarketType MarketType
        {
            get
            {
                return (MarketType)this.marketType;
            }
        }
    }

    public class MarketUpdateMessage
    {
        public string status { get; set; }
        public string marketHash { get; set; }
        public string outcomeOneName { get; set; }
        public string outcomeTwoName { get; set; }
        public string outcomeVoidName { get; set; }
        public string teamOneName { get; set; }
        public string teamTwoName { get; set; }
        public Int32 participantOneId { get; set; }
        public Int32 participantTwoId { get; set; }
        public Int32 type { get; set; }
        public Int64 gameTime { get; set; }
        public decimal line { get; set; }
        public string sportXeventId { get; set; }
        public bool liveEnabled { get; set; }
        public string sportLabel { get; set; }
        public Int32 sportId { get; set; }
        public Int32 leagueId { get; set; }
        public bool homeTeamFirst { get; set; }
        public string leagueLabel { get; set; }
        public string group1 { get; set; }
        public string group2 { get; set; }

        public MarketType MarketType
        {
            get
            {
                return (MarketType)this.type;
            }
        }

        public Sport Sport
        {
            get
            {
                return (Sport)sportId;
            }
        }

        public DateTime StartTime
        {
            get
            {
                return DateTimeOffset.FromUnixTimeSeconds(this.gameTime).UtcDateTime;
            }
        }
    }

    public class TradeUpdateMessage
    {
        [JsonProperty("_id")]
        public string id { get; set; }
        public string baseToken { get; set; }
        public string bettor { get; set; }
        public string stake { get; set; }
        public string odds { get; set; }
        public string orderHash { get; set; }
        public string marketHash { get; set; }
        public bool maker { get; set; }
        public Int64 betTime { get; set; }
        public bool settled { get; set; }
        public bool bettingOutcomeOne { get; set; }
        public string fillHash { get; set; }
        public string status { get; set; }
        public decimal betTimeValue { get; set; }

        public Price Price { get; set; }
        public Price PriceWithCommission { get; set; }
        public decimal? CommissionRate { get; set; }
        public Amount Stake { get; set; }

        public DateTime BetDateTime
        {
            get
            {
                return DateTimeOffset.FromUnixTimeSeconds(this.betTime).UtcDateTime;
            }
        }
    }
}
