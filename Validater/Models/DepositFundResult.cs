namespace Validater.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Validater.CustomAttributes;

    public class DepositFundResult
    {
        [JsonProperty("transactionId")]
        [Condition(CheckName = "transactionId", JsonType = JTokenType.String, Required = true, StringMaxLength = 22)]
        public string TransactionId { get; set; }

        [JsonProperty("totalBalance")]
        [Condition(CheckName = "totalBalance", JsonType = JTokenType.Float, Required = true)]
        public decimal TotalBalance { get; set; }
    }
}
