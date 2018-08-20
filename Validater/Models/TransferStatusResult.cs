namespace Validater.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Validater.CustomAttributes;

    public class TransferStatusResult
    {
        [JsonProperty("memberCode")]
        [Condition(CheckName = "memberCode", JsonType = JTokenType.String, Required = true, StringMaxLength = 15)]
        public string MemberCode { get; set; }

        [JsonProperty("transactionId")]
        [Condition(CheckName = "transactionId", JsonType = JTokenType.String, Required = true, StringMaxLength = 22)]
        public string TransactionId { get; set; }

        [JsonProperty("transactionType")]
        [Condition(CheckName = "transactionType", JsonType = JTokenType.Integer, Required = true)]
        public int TransactionType { get; set; }

        [JsonProperty("transactionStatus")]
        [Condition(CheckName = "transactionStatus", JsonType = JTokenType.Integer, Required = true)]
        public int TransactionStatus { get; set; }

        [JsonProperty("balance")]
        [Condition(CheckName = "balance", JsonType = JTokenType.Float, Required = true)]
        public decimal Balance { get; set; }

        [JsonProperty("currencyCode")]
        [Condition(CheckName = "currencyCode", JsonType = JTokenType.String, Required = true, StringMaxLength = 20)]
        public string CurrencyCode { get; set; }
    }
}
