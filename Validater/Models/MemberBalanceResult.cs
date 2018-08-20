namespace Validater.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Validater.CustomAttributes;

    public class MemberBalanceResult
    {
        [JsonProperty("balance")]
        [Condition(CheckName = "balance", JsonType = JTokenType.Float, Required = true)]
        public decimal Balance { get; set; }

        [JsonProperty("currencyCode")]
        [Condition(CheckName = "currencyCode", JsonType = JTokenType.String, Required = true, StringMaxLength = 3)]
        public string CurrencyCode { get; set; }
    }
}
