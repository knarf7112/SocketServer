using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Validater.Converters;
using Validater.Models;

namespace Validater
{
    class Program
    {
        static void Main(string[] args)
        {
            // 檢查Response返回的JSON格式是否符合Attribute的設定
            string resourceName = "DepositFund";
            string resultText = "{\"carrier\": {},\"data\": {\"memberCode\": \"Member1234\",\"balance\": 120.25,\"CurrencyCode\": \"CNY\",\"refId\": \"ABC123456\" } }";// json string

            var CustomConverter = new ValidationConverter();
            CustomConverter.ShowError = (errors) => { errors.ForEach((err => { Console.Out.WriteLine(resourceName + " : " + err); })); };
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter>() { CustomConverter }
            };


            switch (resourceName)
            {
                case "Launch":
                    var launchResult = JsonConvert.DeserializeObject<Response<string>>(resultText, settings);
                    break;
                case "MemberBalance":
                    var memberBalanceResult = JsonConvert.DeserializeObject<Response<MemberBalanceResult>>(resultText, settings);
                    break;
                case "DepositFund":
                    var depositFundResult = JsonConvert.DeserializeObject<Response<DepositFundResult>>(resultText, settings);
                    break;
                case "WithdrawalFund":
                    var withdrawalFundResult = JsonConvert.DeserializeObject<Response<WithdrawalFundResult>>(resultText, settings);
                    break;
                case "TransferStatus":
                    var transferStatusResult = JsonConvert.DeserializeObject<Response<TransferStatusResult>>(resultText, settings);
                    break;
                case "Wagers":
                    var wagersResult = JsonConvert.DeserializeObject<Response<List<Wagers<Bets<Event>>>>>(resultText, settings);
                    break;
                default:
                    var result = JsonConvert.DeserializeObject<Response<object>>(resultText, settings);
                    break;
            }

            Console.ReadKey();
        }
    }
}
