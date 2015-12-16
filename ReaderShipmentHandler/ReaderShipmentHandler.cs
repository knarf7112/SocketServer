using System;
using System.Linq;
using System.Text;
//
using System.Web;
using Common.Logging;
using System.Diagnostics;
using System.IO;
using Crypto.POCO;
using Newtonsoft.Json;

namespace ReaderShipmentWebHandler
{
    public class ReaderShipmentHandler : IHttpHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ReaderShipmentHandler));

        /// <summary>
        /// 認證資料長度
        /// </summary>
        private static readonly int LoadKeyLength = 50;

        /// <summary>
        /// 從設定檔讀取的連向後台Service的服務名稱
        /// </summary>
        private static readonly string ServiceName = "LoadKeyService";

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            string inputData = string.Empty;
            byte[] backEndResponseData = null;
            byte[] responseData = null;
            Stopwatch timer = new Stopwatch();//耗時計算
            timer.Start();
            #region Request資料轉ASCII字串
            log.Debug("[UserIP]:" + context.Request.UserHostAddress + "\n UserAgent:" + context.Request.UserAgent);
            inputData = GetStringFromInputStream(context);
            log.Debug("[ReaderShipmentHandler Request] Data(length:" + inputData.Length + "):" + inputData);
            context.Response.ContentType = "text/plain";
            context.Response.StatusCode = 200;
            //檢查request資料是否符合規定長度
            if (!String.IsNullOrEmpty(inputData) && inputData.Length == LoadKeyLength)
            {
                backEndResponseData = DoLoadKey(inputData);
                //context.Response.OutputStream.Position = 0;
                if (backEndResponseData != null)
                {
                    responseData = InsertDataLengthToHeader(backEndResponseData);
                    log.Debug("[ReaderShipmentHandler Response] Data(length:" + responseData.Length + "):" + BitConverter.ToString(responseData).Replace("-", ""));
                    context.Response.OutputStream.Write(responseData, 0, responseData.Length);//return 4+32 bytes
                }
                else
                {
                    //Authenticate fail
                    log.Debug("ReaderShipmentHandler Failed");
                    byte[] failResult = new byte[] { 0x00, 0x00, 0x00, 0x00 };//表示數據長度 0
                    context.Response.OutputStream.Write(failResult, 0, failResult.Length);
                }
                log.Debug("[ReaderShipmentHandler]Response Write Finished");
            }
            else
            {
                log.Debug("[ReaderShipmentHandler]Request Error");
                context.Response.OutputStream.Write(Encoding.ASCII.GetBytes("Request Error"), 0, 13);//.Write("Test");
            }
            #endregion

            context.Response.OutputStream.Flush();
            context.Response.OutputStream.Close();
            //context.Response.End();
            timer.Stop();
            log.Debug("[ReaderShipmentHandler] End Response (TimeSpend:" + (timer.ElapsedTicks / (decimal)System.Diagnostics.Stopwatch.Frequency).ToString("f3") + "s)");
            context.ApplicationInstance.CompleteRequest();
        }

        #region Request轉ASCII字串
        /// <summary>
        /// get string from InputStream and Encoding default is ASCII
        /// </summary>
        /// <param name="context">request context</param>
        /// <returns>request http body</returns>
        private static string GetStringFromInputStream(HttpContext context)
        {
            string result = null;
            using (StreamReader sr = new StreamReader(context.Request.InputStream, Encoding.ASCII))
            {
                result = sr.ReadToEnd();
            };
            return result;
        }
        /// <summary>
        /// Convert ASCII string to byte array(length:50)
        /// </summary>
        /// <param name="inputData">challenge Data and parameters</param>
        /// <returns>RanB||E(RanA||RanBRol8)||E(iv,RanARol8)||RanAStartIndex</returns>
        private static byte[] DoLoadKey(string inputData)
        {
            byte[] requestBytes = new byte[25];
            byte[] result = null;
            requestBytes[0] = byte.Parse(inputData.Substring(0, 2));            //keyLabel:0~1     => "32" => byte{0x20}
            requestBytes[1] = byte.Parse(inputData.Substring(2, 2));            //KeyVersion:2~3   => "00" => byte{0x00}
            byte[] uid = StringToByteArray(inputData.Substring(4, 14));         //uid:4~17  => "04873ABA8D2C80" => byte{0x04,0x87,0x3A,0xBA,0x8D,0x2C,0x80}
            byte[] deviceId = StringToByteArray(inputData.Substring(18, 32));   //deviceId:18~50 => "4EF61041ABE8B0EF8B32A627B19D83AA" => byte{0x4E,0xF6,0x10,0x41,0xAB,0xE8,0xB0,0xEF,0x8B,0x32,0xA6,0x27,0xB1,0x9D,0x83,0xAA}
            Buffer.BlockCopy(uid, 0, requestBytes, 2, uid.Length);
            Buffer.BlockCopy(deviceId, 0, requestBytes, (2 + uid.Length), deviceId.Length);
            result = DoLoadKey(requestBytes);
            return result;
        }

        /// <summary>
        /// hex string to byte array
        /// </summary>
        /// <param name="hex">hex data</param>
        /// <returns></returns>
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
        #endregion

        #region Request轉Byte Array
        private static byte[] GetBytesFromInputStream(HttpContext context)
        {
            byte[] buffer = new byte[0x1000];
            int readLength = context.Request.InputStream.Read(buffer, 0, buffer.Length);
            Array.Resize(ref buffer, readLength);
            return buffer;
        }
        /// <summary>
        /// Run 3 Pass Authenticate Flow by Byte Array(length:25)
        /// </summary>
        /// <param name="inputData">challenge Data and parameters</param>
        /// <returns>RanB||E(RanA||RanBRol8)||E(iv,RanARol8)||RanAStartIndex</returns>
        private static byte[] DoLoadKey(byte[] inputData)
        {
            byte[] result = null;
            if (inputData.Length != 25)
                return result;                        //ha ha
            string keyLabel = "2ICH3F0000" + inputData[0].ToString("D2") + "A";             //byte[0]
            string KeyVersion = inputData[1].ToString();                                    //byte[1]
            string uid = BitConverter.ToString(inputData, 2, 7).Replace("-", "");           //byte[2~8]
            string deviceId = BitConverter.ToString(inputData, 9, 16).Replace("-", "");     //byte[9~24]
            EskmsKeyPOCO response = null;
            EskmsKeyPOCO request = new EskmsKeyPOCO()
            {
                Input_KeyLabel = keyLabel,
                Input_KeyVersion = KeyVersion,
                Input_UID = uid,
                Input_DeviceID = deviceId
            };
            log.Debug("");
            response = GetResponse(request);
            if (response != null && response.Output_DiversKey != null)
            {
                result = new byte[response.Output_DiversKey.Length];
                Buffer.BlockCopy(response.Output_DiversKey, 0, result, 0, response.Output_DiversKey.Length);//Copy Divers Key in Result

                log.Debug("DiversKey:" + BitConverter.ToString(response.Output_DiversKey).Replace("-", ""));
            }

            return result;
        }

        /// <summary>
        /// 連線後端AP並取得output data
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private static EskmsKeyPOCO GetResponse(EskmsKeyPOCO request)
        {
            string requestStr = null;
            byte[] requestBytes = null;
            string responseStr = null;
            byte[] responseBytes = null;
            string ip = null;
            int port = -1;
            int sendTimeout = -1;
            int receiveTimeout = -1;
            string serverConfig = null;
            string[] configs = null;
            EskmsKeyPOCO response = null;
            //*********************************
            //取得連線後台的WebConfig設定資料
            serverConfig = ConfigGetter.GetValue(ServiceName);
            log.Debug(m => { m.Invoke(ServiceName + ":" + serverConfig); });
            if (serverConfig != null)
            {
                configs = serverConfig.Split(':');
                ip = configs[0];
                port = Convert.ToInt32(configs[1]);
                sendTimeout = Convert.ToInt32(configs[2]);
                receiveTimeout = Convert.ToInt32(configs[3]);
            }
            else
            {
                log.Error("要連結的目的地設定資料不存在:" + ServiceName);
                return null;
            }
            //*********************************
            try
            {
                using (SocketClient.Domain.SocketClient connectToAP = new SocketClient.Domain.SocketClient(ip, port, sendTimeout, receiveTimeout))
                {
                    log.Debug("開始連線後端服務:" + serverConfig);
                    if (connectToAP.ConnectToServer())
                    {
                        //UTF8(JSON(POCO))=>byte array and send to AP
                        requestStr = JsonConvert.SerializeObject(request);
                        log.Debug(m => m("[ReaderShipmentHandler]Request JsonString({0}): {1}", ServiceName, requestStr));
                        requestBytes = Encoding.UTF8.GetBytes(requestStr);//Center AP used UTF8
                        responseBytes = connectToAP.SendAndReceive(requestBytes);
                        if (responseBytes != null)
                        {
                            responseStr = Encoding.UTF8.GetString(responseBytes);
                            response = JsonConvert.DeserializeObject<EskmsKeyPOCO>(responseStr);
                            //Byte[] 會被JSON轉成Base64格式
                            log.Debug(m =>
                            {
                                m.Invoke("[ReaderShipmentHandler]Response JsonString:\n DiversKey:{0}",
                                    BitConverter.ToString(response.Output_DiversKey).Replace("-", ""));
                            });
                        }
                        else
                        {
                            //Byte[] 會被JSON轉成Base64格式
                            log.Debug(m => { m.Invoke("[ReaderShipmentHandler]Response JsonString: null"); });
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                log.Error("後台連線異常:" + ex.Message);
            }
            return response;
        }
        #endregion

        /// <summary>
        /// 將數據前面增加數據長度(4 bytes) ex:data={0x01,0x02,0x03,0x04} => {0x04,0x00,0x00,0x00,  0x01,0x02,0x03,0x04} 
        /// </summary>
        /// <param name="data">數據</param>
        /// <returns></returns>
        private byte[] InsertDataLengthToHeader(byte[] data)
        {
            byte[] result = null;
            byte[] length = new byte[4];
            //位移bit
            for (int i = 0; i < 4; i++)
            {
                length[i] = Convert.ToByte((data.Length << (8 * (3 - i))) >> 24);//取int32(4*8bit)的個別byte數據
            }
            result = length.Concat(data).ToArray();
            return result;
        }
    }
}
