namespace Validater.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Validater.CustomAttributes;

    public class Wagers<T>
    {
        [JsonProperty("memberCode")]
        [Condition(CheckName = "memberCode", JsonType = JTokenType.String, Required = true, StringMaxLength = 15)]
        public string MemberCode { get; set; }

        [JsonProperty("currencyCode")]
        [Condition(CheckName = "currencyCode", JsonType = JTokenType.String, Required = true, StringMaxLength = 3)]
        public string CurrencyCode { get; set; }

        [JsonProperty("ipAddress")]
        [Condition(CheckName = "ipAddress", JsonType = JTokenType.String, Required = true, StringMaxLength = 39)]
        public string IpAddress { get; set; }

        [JsonProperty("id")]
        [Condition(CheckName = "id", JsonType = JTokenType.String, Required = true, StringMaxLength = 50)]
        public string Id { get; set; }

        [JsonProperty("createTime")]
        [Condition(CheckName = "createTime", JsonType = JTokenType.Date, Required = true)]
        public DateTime CreateTime { get; set; }

        [JsonProperty("settleTime")]
        [Condition(CheckName = "settleTime", JsonType = JTokenType.Date, Required = true)]
        public DateTime SettleTime { get; set; }

        [JsonProperty("wagerStatus")]
        [Condition(CheckName = "wagerStatus", JsonType = JTokenType.Integer, Required = true)]
        public int WagerStatus { get; set; }

        [JsonProperty("stake")]
        [Condition(CheckName = "stake", JsonType = JTokenType.Float, Required = true)]
        public decimal Stake { get; set; }

        [JsonProperty("returnAmount")]
        [Condition(CheckName = "returnAmount", JsonType = JTokenType.Float, Required = true)]
        public decimal ReturnAmount { get; set; }

        [JsonProperty("channel")]
        [Condition(CheckName = "channel", JsonType = JTokenType.Integer, Required = true)]
        public int Channel { get; set; }

        [JsonProperty("oddsType")]
        [Condition(CheckName = "oddsType", JsonType = JTokenType.Integer, Required = true)]
        public int OddsType { get; set; }

        [JsonProperty("odds")]
        [Condition(CheckName = "odds", JsonType = JTokenType.Float, Required = true)]
        public decimal Odds { get; set; }

        [JsonProperty("bets")]
        [Condition(CheckName = "bets", JsonType = JTokenType.Array, Required = true)]
        public List<T> Bets { get; set; }
    }

    public class Bets<T>
    {
        [JsonProperty("id")]
        [Condition(CheckName = "id", JsonType = JTokenType.String, Required = true, StringMaxLength = 50)]
        public string Id { get; set; }

        [JsonProperty("seq")]
        [Condition(CheckName = "seq", JsonType = JTokenType.Integer, Required = true)]
        public int Seq { get; set; }

        [JsonProperty("betStatus")]
        [Condition(CheckName = "betStatus", JsonType = JTokenType.Integer, Required = true)]
        public int BetStatus { get; set; }

        [JsonProperty("stake")]
        [Condition(CheckName = "stake", JsonType = JTokenType.Float, Required = true)]
        public decimal Stake { get; set; }

        [JsonProperty("returnAmount")]
        [Condition(CheckName = "returnAmount", JsonType = JTokenType.Float, Required = true)]
        public decimal ReturnAmount { get; set; }

        [JsonProperty("odds")]
        [Condition(CheckName = "odds", JsonType = JTokenType.Float, Required = true)]
        public decimal Odds { get; set; }

        [JsonProperty("betType")]
        [Condition(CheckName = "betType", JsonType = JTokenType.String, Required = true, StringMaxLength = 50)]
        public string BetType { get; set; }

        [JsonProperty("selection")]
        [Condition(CheckName = "selection", JsonType = JTokenType.String, Required = true, StringMaxLength = 50)]
        public string Selection { get; set; }

        [JsonProperty("handicap")]
        [Condition(CheckName = "handicap", JsonType = JTokenType.Float)]
        public decimal Handicap { get; set; }

        [JsonProperty("period")]
        [Condition(CheckName = "period", JsonType = JTokenType.String, StringMaxLength = 50)]
        public string Period { get; set; }

        [JsonProperty("group")]
        [Condition(CheckName = "group", JsonType = JTokenType.String, StringMaxLength = 50)]
        public string Group { get; set; }

        [JsonProperty("remark")]
        [Condition(CheckName = "remark", JsonType = JTokenType.String, StringMaxLength = 50)]
        public string Remark { get; set; }

        [JsonProperty("event")]
        [Condition(CheckName = "event", JsonType = JTokenType.Object, Required = true)]
        public List<T> Event { get; set; }
    }

    public class Event
    {
        [JsonProperty("gameType")]
        [Condition(CheckName = "gameType", JsonType = JTokenType.Integer, Required = true)]
        public int gameType { get; set; }

        [JsonProperty("id")]
        [Condition(CheckName = "id", JsonType = JTokenType.String, Required = true, StringMaxLength = 50)]
        public string Id { get; set; }

        [JsonProperty("name")]
        [Condition(CheckName = "name", JsonType = JTokenType.String, StringMaxLength = 50)]
        public string Name { get; set; }

        [JsonProperty("startTime")]
        [Condition(CheckName = "startTime", JsonType = JTokenType.Date, Required = true)]
        public DateTime StartTime { get; set; }

        [JsonProperty("endTime")]
        [Condition(CheckName = "endTime", JsonType = JTokenType.Date, Required = true)]
        public DateTime EndTime { get; set; }

        [JsonProperty("results")]
        [Condition(CheckName = "results", JsonType = JTokenType.String, Required = true, StringMaxLength = 256)]
        public string Results { get; set; }

        [JsonProperty("tag")]
        [Condition(CheckName = "tag", JsonType = JTokenType.String, StringMaxLength = 50)]
        public string Tag { get; set; }
    }
}
