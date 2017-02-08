using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
//
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Test_Android_GCM_PushNotification
{
    class Program
    {
        static string GCM_Url = "https://android.googleapis.com/gcm/send";
        static void Main(string[] args)
        {
            string jsonStr =string.Format("{{\"alert\":\"{0}\",\"badge\":{1},\"NotifyMessageID\":{2},\"MID\":{3},\"subject\":\"{4}\",\"action-loc-key\":\"{5}\"}}",
                                    "內容",
                                    "12",
                                    "985",
                                    "1234567",
                                    "抬頭",
                                    "自定義命令");

            JObject jObj = JObject.Parse(jsonStr);
            var qq = jObj["dd"];

            Console.ReadKey();
        }
        //1.api_key, 2.msg 3.RegistrationID(deviceID)
        static string HttpPostToGCM(string RegistrationID, string api_key, string msg) 
        {
            var postObj = new
            {
                data = new
                {
                    message = msg
                },
                registration_ids = new string[] { RegistrationID }
            };

            string postStr = JsonConvert.SerializeObject(postObj);//parse to json string
            Console.WriteLine("Post Data:" + postStr);
            byte[] postArr = Encoding.UTF8.GetBytes(postStr);

            //initial request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GCM_Url);
            request.Method = "POST";
            request.ContentType = "application/json;charset=utf-8;";
            request.Headers.Add(string.Format("Authorization: key={0}", api_key));
            request.ContentLength = postArr.Length;

            Stream reqStream = request.GetRequestStream();
            reqStream.Write(postArr, 0, postArr.Length);
            reqStream.Flush();


            string responseStr = string.Empty;
            //get response 
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) 
            {
                using (StreamReader resStream = new StreamReader(response.GetResponseStream()))
                {
                    responseStr = resStream.ReadToEnd();
                }
            }

            return "";
        }
    }
}
