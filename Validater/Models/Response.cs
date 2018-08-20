
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Validater.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Validater.CustomAttributes;

    public class Response<T>
    {
        [JsonProperty("carrier")]
        [Condition(CheckName = "carrier", JsonType = JTokenType.Object)]
        public object Carrier { get; set; }

        [JsonProperty("code")]
        [Condition(CheckName = "code", Required = true, JsonType = JTokenType.String, StringMaxLength = 10)]
        public string Code { get; set; }

        [JsonProperty("msg")]
        [Condition(CheckName = "msg", Required = true, JsonType = JTokenType.String, StringMaxLength = 200)]
        public string Msg { get; set; }

        [JsonProperty("data")]
        [Condition(CheckName = "data", Required = true, JsonType = JTokenType.Object)]
        public T Data { get; set; }
    }
}
