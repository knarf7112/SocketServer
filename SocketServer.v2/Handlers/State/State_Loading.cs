using System;
using System.Text;
//Log
using Common.Logging;
//POCO
using LOLCommon;
//Msg Parser
using Kms2.Crypto.Utility;
//http Client
using System.Net.Http;

namespace SocketServer.v2.Handlers.State
{
    /// <summary>
    /// 處理狀態:一般加值
    /// </summary>
    public class State_Loading : IState
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(State_Loading));
        //
        private string readerId;
        private string transDateTime;
        private string mac;
        private string amount;
        /// <summary>
        /// 處理邏輯
        /// </summary>
        /// <param name="handler"></param>
        public void Handle(ClientRequestHandler handler)
        {
            try
            {
                LOL_Domain request = null;
                bool doMac = true;
                LOL_Domain response = null;
                byte[] responseArr = null;
                log.Debug(m => m("[State_Loading] Request(ASCII):{0}", ClientRequestHandler.asciiOctets2String(handler.Request)));
                //1.驗證Mac
                if (this.CheckMac(CheckMacContainer.LOLReqMsgUtility, handler.Request))
                {
                    //2.parse request
                    request = this.ParseRequest(CheckMacContainer.AOLReqMsgUtility, handler.Request);
                    //3.send to Back-End request and get response
                    response = this.GetResponse(request);
                    if (response == null || string.IsNullOrEmpty(response.LOAD_RC))
                    {
                        //back-End Error
                        response = new LOL_Domain()
                        {
                            LOAD_SN = "99999999",
                            LOAD_RC = "990001"
                        };
                    }
                }
                else
                {
                    //MAC驗證失敗
                    doMac = false;
                    response = new LOL_Domain()
                    {
                        LOAD_SN = "99999999",
                        LOAD_RC = "990001"
                    };
                }

                //4. parse response byte array(create mac)
                responseArr = this.ParseResponse(CheckMacContainer.LOLRespMsgUtility, response, handler.Request, doMac);

                log.Info(m => m("Response(length:{0}):{1}", responseArr.Length, BitConverter.ToString(responseArr).Replace("-", "")));
                int sendLength = handler.ClientSocket.Send(responseArr);
                if (sendLength != responseArr.Length)
                {
                    log.Error(m => m("Response[length:{0}] not equal Actual Send Response[Length:{1}]", responseArr.Length, sendLength));
                }
            }
            catch (Exception ex)
            {
                log.Error(m => m("[State_Loading][Handle] Error: {0} \r\n{1}", ex.Message, ex.StackTrace));
            }
            finally
            {
                handler.ServiceState = new State_Exit();
            }
        }

        /// <summary>
        /// 檢驗Request的MAC
        /// </summary>
        /// <param name="msgUtility">Request Msg Parser</param>
        /// <param name="msgBytes">Origin Request byte array</param>
        /// <returns>true:success/false:failed</returns>
        protected virtual bool CheckMac(IMsgUtility msgUtility, byte[] msgBytes)
        {
            bool result = false;
            try
            {
                log.Debug(m => m("1.開始驗證MAC"));
                //reader id 取前面16個字串
                this.readerId = msgUtility.GetStr(msgBytes, "ReaderId").Substring(0, 16);
                //取交易時間
                this.transDateTime = msgUtility.GetStr(msgBytes, "TransDateTime"); ;
                //取後面的mac:要用來檢查data部分是否有被串改
                this.mac = msgUtility.GetStr(msgBytes, "Mac");
                //錢
                this.amount = msgUtility.GetStr(msgBytes, "Amount");
                //將data的部分取出作SHA1
                byte[] hashData = msgUtility.GetBytes(msgBytes, "HeaderVersion", "Mac");
                byte[] hashResult = CheckMacContainer.HashWorker.ComputeHash(hashData);
                //開始檢查hash過的data部分是否與mac値一致
                result = CheckMacContainer.Mac2Manager.ChkInitMac(this.readerId, this.transDateTime, hashResult, this.mac);
                log.Debug(m => m("2.結束驗證MAC:{0}", result.ToString()));
                return result;
            }
            catch (Exception ex)
            {
                log.Error(m => m("[State_Loading][CheckMac] Error: {0} \r\n{1}", ex.Message, ex.StackTrace));
                return false;
            }
        }

        /// <summary>
        /// Request依據規格書截取需要的資料轉成POCO
        /// </summary>
        /// <param name="msgUtility">Request Msg Parser</param>
        /// <param name="msgBytes">Origin Request byte array</param>
        /// <returns>POCO</returns>
        protected virtual LOL_Domain ParseRequest(IMsgUtility msgUtility, byte[] msgBytes)
        {
            log.Debug("3.開始轉換一般加值Request物件");
            LOL_Domain oLDomain = new LOL_Domain();
            //ComType:0332
            oLDomain.COM_TYPE = msgUtility.GetStr(msgBytes, "ReqType");

            oLDomain.POS_FLG = msgUtility.GetStr(msgBytes, "ReaderType");

            oLDomain.READER_ID = this.readerId;//CheckMacContainer.AOLReqMsgUtility.GetStr(msgBytes, "ReaderId").Substring(0, 16);

            oLDomain.MERC_FLG = msgUtility.GetStr(msgBytes, "MercFlg");

            oLDomain.STORE_NO = msgUtility.GetStr(msgBytes, "StoreNo");

            oLDomain.REG_ID = msgUtility.GetStr(msgBytes, "RegId");

            // modify StoreNo and RegId
            if ("SET".Equals(oLDomain.MERC_FLG))
            {
                oLDomain.STORE_NO = oLDomain.STORE_NO.Substring(2, 6);
                oLDomain.REG_ID = oLDomain.REG_ID.Substring(1, 2);
            }

            //
            oLDomain.LOAD_AMT = Convert.ToInt32(this.amount);
            //
            oLDomain.ICC_NO = msgUtility.GetStr(msgBytes, "CardNo");

            log.Debug("4.結束轉換一般加值Request物件");
            return oLDomain;
        }

        /// <summary>
        /// Response物件建立MAC並依據規格書轉換成byte[]
        /// </summary>
        /// <param name="msgUtility">Response Msg Parser</param>
        /// <param name="response">後端給的POCO</param>
        /// <param name="request">Origin Request byte array</param>
        /// <param name="doMAC">是否產生MAC:驗證失敗就使用來源的mac</param>
        /// <returns>response byte array</returns>
        protected virtual byte[] ParseResponse(IMsgUtility msgUtility, LOL_Domain response, byte[] request, bool doMAC = true)
        {
            log.Debug(m => m("7.轉換後台Response物件並建立MAC並轉成Response Byte[]"));
            byte[] rspResult = new byte[request.Length];
            Buffer.BlockCopy(request, 0, rspResult, 0, request.Length);//

            string amount = msgUtility.GetStr(request, "Amount");
            // modify request to response
            msgUtility.SetStr("02", rspResult, "Communicate");
            msgUtility.SetStr(response.LOAD_RC, rspResult, "ReturnCode");
            msgUtility.SetStr(response.LOAD_SN, rspResult, "CenterSeqNo");
            msgUtility.SetStr(this.amount, rspResult, "Amount");

            msgUtility.SetStr(this.transDateTime, rspResult, "TransDateTime");
            //是否產生mac
            if (doMAC)
            {
                // fetch sha1 data
                this.SetMAC(msgUtility, rspResult, this.readerId, this.transDateTime);
            }
            else
            {
                //若Mac驗證失敗,則使用來源mac簡易回傳
                msgUtility.SetStr(this.mac, rspResult, "Mac");
            }
            // padding data
            this.SetPadding(msgUtility, rspResult);
            return rspResult;
        }
        /// <summary>
        /// Response寫入MAC
        /// </summary>
        /// <param name="resMsgUility">Response Parser</param>
        /// <param name="response">response byte array</param>
        /// <param name="reader_id">卡機裝置ID(length:16)</param>
        /// <param name="transTime">交易時間(length:14)</param>
        protected virtual void SetMAC(IMsgUtility resMsgUility, byte[] response, string reader_id, string transTime)
        {
            // fetch sha1 data
            byte[] hashDataBytes = resMsgUility.GetBytes(response, "HeaderVersion", "Mac");
            byte[] sha1Result = CheckMacContainer.HashWorker.ComputeHash(hashDataBytes);
            string mac = CheckMacContainer.Mac2Manager.GetAuthMac(reader_id, transTime, sha1Result);
            resMsgUility.SetStr(mac, response, "Mac");
            log.Debug(m => m("8.Response mac: {0}", mac));
        }

        /// <summary>
        /// 設定Response的Padding部分
        /// </summary>
        /// <param name="resMsgUility">Response Msg Parser</param>
        /// <param name="response">要寫入的response byte array</param>
        protected virtual void SetPadding(IMsgUtility resMsgUility, byte[] response)
        {
            // padding data
            byte[] spacePadding = CheckMacContainer.ByteWorker.Fill(resMsgUility.GetTag("DataPadding").Length, 0x20);
            resMsgUility.SetBytes(spacePadding, response, "DataPadding");
            int length = response.Length;
            log.Debug(m => m("9.Response Padding End (array length:{0})", length));
        }

        /// <summary>
        /// 送到後台的request物件並取回response物件
        /// </summary>
        /// <param name="request">request poco</param>
        /// <returns>response poco</returns>
        protected virtual LOL_Domain GetResponse(LOL_Domain request)
        {
            LOL_Domain result = null;
            HttpClient client = null;
            HttpRequestMessage requestMsg = null;
            string requestJSONstr = string.Empty;
            string jsonData = string.Empty;
            try
            {
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                requestJSONstr = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                log.Debug(m => m("5.[Loading][Send] to Back-End Data: {0}", requestJSONstr));

                string url = ConfigLoader.GetSetting(ConType.Loading);
                timer.Start();
                client = new HttpClient() { Timeout = new TimeSpan(0, 0, 10) };
                requestMsg = new HttpRequestMessage(HttpMethod.Post, url);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                requestMsg.Content = new StringContent(requestJSONstr, Encoding.UTF8, "application/json");
                client.SendAsync(requestMsg).ContinueWith(responseTask =>
                {
                    HttpResponseMessage response = responseTask.Result as HttpResponseMessage;
                    return response.Content.ReadAsByteArrayAsync().Result;
                }).ContinueWith(dataTask =>
                {
                    byte[] data = dataTask.Result as byte[];
                    jsonData = Encoding.UTF8.GetString(data);
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<LOL_Domain>(jsonData);
                }).Wait(5000);
                timer.Stop();
                log.Debug(m => m("6.[Loading][Receive]Back-End Response(TimeSpend:{1}ms): {0}", jsonData, timer.ElapsedMilliseconds));


                return result;
            }
            catch (Exception ex)
            {
                log.Error("[GetResponse]Send Back-End Error: " + ex.Message + " \r\n" + ex.StackTrace);
                return null;
            }
            finally
            {

            }
        }
    }
}
