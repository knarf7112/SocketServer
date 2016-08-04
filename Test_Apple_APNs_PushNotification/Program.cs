using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Net.Sockets;
//
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;

namespace Test_Apple_APNs_PushNotification
{
    class Program
    {
        static X509Certificate certificate;
        static DateTime? Expiration;
        static DateTime DoNotStore;
        static DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        static string DeviceToken = "63974870e3c059c8e8dae324d734a87189552d0cab0864b58dd785be299baff6";//"7408e047feed9fff7990d3cc9204fb80f43cf1aeb8268e7d5c3b799737e3e0a6"; ////1002728(iphone6s+ 只能收款)憑證(dev)allpay_apns_dev.p12用的
                                    //"c1564dd73cd73a003d2ad143d96c9e6d651f8b48b45ba8c0ae9c5db87513fde8";
                                      //"7408e047feed9fff7990d3cc9204fb80f43cf1aeb8268e7d5c3b799737e3e0a6"; //憑證(prod)aps_production_allpay.p12用的
                                      //"b25ba4c8b4b01a3a101592615275d7a5a7231a107d6952f4cdd0afa08749bb9d";//1002728(iphone6s+ 只能收款)
                                    //"f6c0ad4ac5ed95d339af637976efb6df61f3619ed3dd1297fd7e489fe9494766";//1000298(白色小支的iphone--只能付款)
        //  private static string DeviceToken = "64d73e7b0815f7c22d7a412dbde7c4f4477ada6fdc4ee8d57db8ee536706dea6";
        //ref:http://dbhills.blogspot.tw/2015/01/ciosapns.html
        static void Main(string[] args)
        {
            //MID:1002728, Sum:2, MsgID:9257, Subj:【羅小姐】收款成功2元, Title:【羅小姐】收款成功2元, RegID:b25ba4c8b4b01a3a101592615275d7a5a7231a107d6952f4cdd0afa08749bb9d
            /*
            byte[] b1 = new byte[0];
            byte[] b2 = new byte[] { };
            DateTime d = DateTime.Now;
            DateTime? date1 =  null;
            DateTime date2 = default(DateTime);
            var check = (date1 != date2);
            var check2 = (date2.Equals(date1));
            int data1 = IPAddress.HostToNetworkOrder(514);
            byte[] aaa = BitConverter.GetBytes(data1);
            */
            int Count = 13;
            Timer timer = new Timer(new TimerCallback((object obj) => {
                //int count = (int)obj;
                Count++;
                AppleNotificationPayload payload = new AppleNotificationPayload();
                payload.Alert.Body = String.Format("Test{0} 使用dev憑證 + (gateway.sandbox.push.apple.com)", Count.ToString("##0"));
                payload.Badge = Count;
                payload.Sound = "default";


                string json = payload.ToJson();
                Console.WriteLine("Send Json: " + json);

                string sendUTF8Str = SendAPNS(DeviceToken, json);
                Console.WriteLine("" + sendUTF8Str);
            }), Count,10,2000);
            /*
            AppleNotificationPayload payload = new AppleNotificationPayload();
            payload.Alert.Body = "Test13 使用dev憑證 + (gateway.sandbox.push.apple.com)";
            payload.Badge = 4;
            payload.Sound = "default";


            string json = payload.ToJson();
            Console.WriteLine("Send Json: " + json);

            string sendUTF8Str = SendAPNS(DeviceToken, json);
            Console.WriteLine("" + sendUTF8Str);
             */
            Console.ReadKey();
        }

        static string SendAPNS(string deviceToken, string content)
        {
            //ref:https://msdn.microsoft.com/en-us/library/txafckwd.aspx
            //ref2:http://stackoverflow.com/questions/16101100/string-format-input-string-was-not-in-correct-format-for-string-with-curly-brack
            //要多加雙括號"{{"才能讓參數寫入string的format
            string jsonContent = String.Format("{{\"aps:\":{{\"alert\":\"{0}\",\"badge\":8,\"sound\":\"default\"}}}}",deviceToken);
            //Json: { MID = 1000242, MsgID = 12345, RegID = "c1564dd73cd73a003d2ad143d96c9e6d651f8b48b45ba8c0ae9c5db87513fde8", Subj = "測試12 主題一:88個badge", Sum = 88, Title = "test2 Content" };
            //str = "{\"aps\":{\"alert\":\"" + s2 + "\",\"badge\":10,\"sound\":\"default\"}}";
            string hostIP = "gateway.sandbox.push.apple.com";//;//"gateway.push.apple.com";//feedback.sandbox.push.apple.com//"feedback.sandbox.push.apple.com";
            int port = 2195;//2196;
            string password = "AllPay";//AllPay

            //certificate load 需要去Apple申請App的憑證才有此檔
            string certificatepath = "allpay_apns_dev.p12";//"AllPayEPAPNS.p12" ;//"aps_production_allpay.p12";// //"allpay.p12";//bin/debug
            string certificateFullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppleCertificate", certificatepath);

            certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(File.ReadAllBytes(certificateFullPath), password,X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            var certificates = new System.Security.Cryptography.X509Certificates.X509Certificate2Collection();
            certificates.Add(certificate);

            //使用TCP Cient來建立connect(就是走簡易的http)
            TcpClient apnsClient = new TcpClient();
            Stopwatch timer = new Stopwatch();
            try
            {
                timer.Start();
                apnsClient.Connect(hostIP, port);
            }
            catch (SocketException ex)
            {
                timer.Stop();
                Console.WriteLine("TimeSpend:{0}ms  ex:{1}", timer.ElapsedMilliseconds, ex.Message);
            }
            //主要認證憑證就是這段,可以使用event認證兩方的憑證,目前APNs這邊都只使用Apple給的憑證
            System.Net.Security.SslStream apnsStream = new System.Net.Security.SslStream(apnsClient.GetStream(),
                                                                                        false,
                                                                                        new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate),
                                                                                        new System.Net.Security.LocalCertificateSelectionCallback(SelectLocalCertificate));

            try
            {
                apnsStream.AuthenticateAsClient(hostIP, certificates, System.Security.Authentication.SslProtocols.Tls, false);
                timer.Stop();
                Console.WriteLine("做完認證的TimeSpend:{0}ms", timer.ElapsedMilliseconds);
            }
            catch(System.Security.Authentication.AuthenticationException ex)
            {
                Console.WriteLine("error:" + ex.Message);
            }


            if (!apnsStream.IsMutuallyAuthenticated)
            {
                Console.WriteLine("error:" + "Ssl Stream Failed to Authenticate");
            }

            if (!apnsStream.CanWrite)
            {
                Console.WriteLine("error:" + "Ssl Stream is not Writable");
                return "";
            }
            //需要取得Apple手機給的token來當作裝置的識別碼,送的格式參考APPLE規定的JSON
            byte[] message = ToBytes(deviceToken, content);
            apnsStream.Write(message);//這邊就可以開始送資料了
            return Encoding.UTF8.GetString(message);
        }
        //轉換成Apple指定的binary格式
        //https://developer.apple.com/library/ios/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/Appendixes/LegacyFormat.html#//apple_ref/doc/uid/TP40008194-CH105-SW1
        static byte[] ToBytes(string deviceTokenHex,string content)
        {
            Console.WriteLine("ToBytes String: " + content);
            //1. command => byte[]{ 0x01 }
            byte[] command = new byte[]{ 0x01 };
            //System.Threading.CountdownEvent s = new System.Threading.CountdownEvent(1);
            
            int identifier = 0;
            //2.Identifier(2 bytes)
            byte[] identifierBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(identifier));
            int expiryTimeStamp = -1;
            if (Expiration != DoNotStore)
            {
                DateTime concreteExpireDateUtc = (Expiration ?? DateTime.UtcNow.AddMonths(1).ToUniversalTime());
                TimeSpan epochTimeSpan = concreteExpireDateUtc - UNIX_EPOCH;
                expiryTimeStamp = (int)epochTimeSpan.TotalSeconds;
            }
            //3. Expiry(4 bytes)
            byte[] expiry = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(expiryTimeStamp));

            //5. DeviceToken(32 bytes)
            byte[] deviceToken = new byte[DeviceToken.Length/2];
            for(int i = 0; i < deviceToken.Length;i++)
                deviceToken[i] = Byte.Parse(DeviceToken.Substring(i*2,2),System.Globalization.NumberStyles.HexNumber);

            //4. DeviceToken Length(4 bytes)
            byte[] deviceTokenSize = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(Convert.ToInt16(deviceToken.Length)));//因為為Big endian所以要顛倒

            //7. Payload
            byte[] payload = Encoding.UTF8.GetBytes(content);
            //6. Payload Length
            byte[] payloadSize = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(Convert.ToInt16(payload.Length)));//因為為Big endian所以要顛倒

            List<byte[]> notificationParts = new List<byte[]>();
            notificationParts.Add(command);
            notificationParts.Add(identifierBytes);
            notificationParts.Add(expiry);
            notificationParts.Add(deviceTokenSize);
            notificationParts.Add(deviceToken);
            notificationParts.Add(payloadSize);
            notificationParts.Add(payload);

            return BuildBufferFrom(notificationParts);
        }

        static byte[] BuildBufferFrom(IList<byte[]> bufferParts)
        {
            int length = 0;
            foreach (byte[] bytes in bufferParts)
                length += bytes.Length;

            byte[] result = new byte[length];
            for (int i = 0, j = 0; i < bufferParts.Count; i++)
            {
                Buffer.BlockCopy(bufferParts[i], 0, result, j, bufferParts[i].Length);
                j += (bufferParts[i].Length);
            }


            //int position = 0;
            //for (int i = 0; i < bufferParts.Count; i++)
            //{
            //    byte[] part = bufferParts[i];
            //    Buffer.BlockCopy(bufferParts[i], 0, result, position, part.Length);
            //    position += part.Length;
            //}

            return result;
        }

        static byte[] HexToBytes(string hex)
        {
            Regex reg = new Regex("\b[0-9A-F]+",RegexOptions.IgnoreCase); 
            if(!reg.IsMatch(hex))
                throw new ArgumentException("have valid string");

            byte[] result = new byte[hex.Length / 2];


            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Byte.Parse(hex.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }

            return result;
        }

        //驗證Server端X509認證
        static bool ValidateServerCertificate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;// Dont care about server's cert
        }

        static X509Certificate SelectLocalCertificate(object sender, string targetHost, 
            X509CertificateCollection localCertificates,
            X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return certificate;
        }
    }
}
