using System;
using System.Linq;
using System.Text;
//
using Common.Logging;
using System.Diagnostics;
using System.Web;
using System.IO;
using Crypto.POCO;
using Newtonsoft.Json;

namespace ReaderShipmentWebHandler
{
    public class ReaderShipmentTxLogHandler : IHttpHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ReaderShipmentTxLogHandler));

        /// <summary>
        /// Request資料長度(轉完ASCII後)
        /// </summary>
        private static readonly int LoadKeyTxLogLength = 622;

        /// <summary>
        /// 從設定檔讀取的連向後台Service的服務名稱
        /// </summary>
        private static readonly string ServiceName = "LoadKeyTxLogService";

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
            log.Debug("[ReaderShipmentTxLog Request] Data(length:" + inputData.Length + "):" + inputData);
            context.Response.ContentType = "text/plain";
            context.Response.StatusCode = 200;
            //檢查request資料是否符合規定長度
            if (!String.IsNullOrEmpty(inputData) && inputData.Length == LoadKeyTxLogLength)
            {
                backEndResponseData = DoLoadKeyTxLog(inputData);
                //context.Response.OutputStream.Position = 0;
                if (backEndResponseData != null)
                {
                    //前面加4bytes:資料的長度
                    responseData = InsertDataLengthToHeader(backEndResponseData);
                    log.Debug("[ReaderShipmentTxLog Response] Data(length:" + responseData.Length + "):" + BitConverter.ToString(responseData).Replace("-", ""));
                    context.Response.OutputStream.Write(responseData, 0, responseData.Length);//return 4+32 bytes
                }
                else
                {
                    //Authenticate fail
                    log.Debug("ReaderShipmentTxLog Failed");
                    byte[] failResult = new byte[] { 0x00, 0x00, 0x00, 0x00 };//表示數據長度 0
                    context.Response.OutputStream.Write(failResult, 0, failResult.Length);
                }
                log.Debug("[ReaderShipmentTxLog]Response Write Finished");
            }
            else
            {
                log.Debug("[ReaderShipmentTxLog]Request Error");
                context.Response.OutputStream.Write(Encoding.ASCII.GetBytes("Request Error"), 0, 13);//.Write("Test");
            }
            #endregion

            context.Response.OutputStream.Flush();
            context.Response.OutputStream.Close();
            //context.Response.End();
            timer.Stop();
            log.Debug("[ReaderShipmentTxLog] End Response (TimeSpend:" + (timer.ElapsedTicks / (decimal)System.Diagnostics.Stopwatch.Frequency).ToString("f3") + "s)");
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
        /// 將Request資料轉成物件傳到後端服務做DB操作(length:334)
        /// </summary>
        /// <param name="inputData"> UID + DeviceId + TxLog</param>
        /// <returns>Ok:0x30*6 / Fail:null or (0x30*5 + 0x31)</returns>
        private static byte[] DoLoadKeyTxLog(string inputData)
        {
            byte[] result = null;
            if (inputData.Length != LoadKeyTxLogLength)
                return result;                        //ha ha
            string sam_uid = inputData.Substring(0,14);                                                 //字串0~13為SAM UID(14 hex string)
            string deviceId = inputData.Substring(14, 32);                                              //字串14~45為DeviceId(32 hex string)
            string txLog = Encoding.ASCII.GetString(StringToByteArray(inputData.Substring(46, 576)));   //字串46~621為TxLog(576 hex string)=>288 string
            //string deviceId = BitConverter.ToString(inputData, 9, 16).Replace("-", "");     //byte[9~24]
            EskmsKeyTxLogPOCO response = null;
            EskmsKeyTxLogPOCO request = new EskmsKeyTxLogPOCO()
            {
                SAM_UID = sam_uid,
                DeviceId = deviceId,
                TestTxLog = txLog
            };
            //log.Debug("");
            response = GetResponse(request);
            if (response != null && response.ReturnCode != null)
            {
                result = Encoding.ASCII.GetBytes(response.ReturnCode);
                //Buffer.BlockCopy(response.ReturnCode, 0, result, 0, response.ReturnCode.Length);//Copy Divers Key in Result

                log.Debug(m=>m("Response Bytes:{0}" , BitConverter.ToString(result).Replace("-", "")));
            }

            return result;
        }

        /// <summary>
        /// 連線後端AP並取得output data
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private static EskmsKeyTxLogPOCO GetResponse(EskmsKeyTxLogPOCO request)
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
            EskmsKeyTxLogPOCO response = null;
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
                        log.Debug(m => m("[ReaderShipmentTxLog]Request JsonString({0}): {1}", ServiceName, requestStr));
                        requestBytes = Encoding.UTF8.GetBytes(requestStr);//Center AP used UTF8
                        responseBytes = connectToAP.SendAndReceive(requestBytes);
                        if (responseBytes != null)
                        {
                            responseStr = Encoding.UTF8.GetString(responseBytes);
                            response = JsonConvert.DeserializeObject<EskmsKeyTxLogPOCO>(responseStr);
                            //Byte[] 會被JSON轉成Base64格式
                            log.Debug(m =>
                            {
                                m.Invoke("[ReaderShipmentTxLog]Response JsonString:{0}", responseStr);
                            });
                        }
                        else
                        {
                            //Byte[] 會被JSON轉成Base64格式
                            log.Debug(m => { m.Invoke("[ReaderShipmentTxLog]Response JsonString: null"); });
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
