using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//LOG
using Common.Logging;
//POCO
using TRANSPORT_MANAGER_Lib;
//Msg Parser
using Kms2.Crypto.Utility;
//Send data to Web API
using System.Net.Http;
//

namespace SocketServer.v2.Handlers.State
{
    /// <summary>
    /// 處理狀態: 取PAM水位額度
    /// </summary>
    public class State_PAMQuota : IState
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(State_PAMQuota));
        /// <summary>
        /// Http Response TimeOut
        /// </summary>
        private static readonly int ResponseTimeout = 30000;
        //
        private string readerId;
        private string transDateTime;
        private string mac;

        /// <summary>
        /// 處理邏輯
        /// </summary>
        /// <param name="handler"></param>
        public void Handle(ClientRequestHandler handler)
        {
            try
            {
                CTGB_Soc_Request request = null;
                bool doMac = true;
                CTGB_Soc_Request response = null;
                byte[] responseArr = null;
                log.Debug(m => m("[State_PAMQuota] {0} Request(ASCII):{1}", handler.ClientSocket.RemoteEndPoint.ToString(), ClientRequestHandler.asciiOctets2String(handler.Request)));
                //1.驗證Mac
                if (this.CheckMac(CheckMacContainer.PAMReqMsgUtility, handler.Request))
                {
                    //2.parse request
                    request = this.ParseRequest(CheckMacContainer.PAMReqMsgUtility, handler.Request);
                    //3.send to Back-End request and get response
                    response = this.GetResponse(request);
                    if (response == null || string.IsNullOrEmpty(response.RETURN_CODE))
                    {
                        //back-End Error
                        response = new CTGB_Soc_Request()
                        {
                            SN = "00000000",
                            RETURN_CODE = "190001",
                            MERCHANT_ID = request.MERCHANT_ID,
                            STORE_NO = request.STORE_NO,
                            REG_ID = request.REG_ID,
                            POS_SEQNO = request.POS_SEQNO,
                            CASHIER_NO = request.CASHIER_NO,
                            READER_ID = request.READER_ID,
                            LOADING_BAL = request.LOADING_BAL,
                            LOADING_SEQ = request.LOADING_SEQ,
                            DATETIME = request.DATETIME
                        };
                    }
                }
                else
                {
                    //MAC驗證失敗
                    doMac = false;
                    response = new CTGB_Soc_Request()
                    {
                        SN = "0000000",
                        RETURN_CODE = "190003"
                    };
                }

                //4. parse response byte array(create mac)
                responseArr = this.ParseResponse(CheckMacContainer.PAMRespMsgUtility, response, handler.Request, doMac);

                log.Info(m => m("Response(length:{0}):{1}", responseArr.Length, BitConverter.ToString(responseArr).Replace("-", "")));
                int sendLength = handler.ClientSocket.Send(responseArr);
                if (sendLength != responseArr.Length)
                {
                    log.Error(m => m("Response[length:{0}] not equal Actual Send Response[Length:{1}]", responseArr.Length, sendLength));
                }
            }
            catch (Exception ex)
            {
                log.Error(m => m("[State_PAMQuota][Handle] Error: {0} \r\n{1}", ex.Message, ex.StackTrace));
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
                this.transDateTime = msgUtility.GetStr(msgBytes, "DateTime"); ;
                //取後面的mac:要用來檢查data部分是否有被串改
                this.mac = msgUtility.GetStr(msgBytes, "Mac");

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
                log.Error(m => m("[State_PAMQuota][CheckMac] Error: {0} \r\n{1}", ex.Message, ex.StackTrace));
                return false;
            }
        }

        /// <summary>
        /// Request依據規格書截取需要的資料轉成POCO
        /// </summary>
        /// <param name="msgUtility">Request Msg Parser</param>
        /// <param name="msgBytes">Origin Request byte array</param>
        /// <returns>POCO</returns>
        protected virtual CTGB_Soc_Request ParseRequest(IMsgUtility msgUtility, byte[] msgBytes)
        {
            log.Debug("3.開始轉換取PAM額度Request物件");
            CTGB_Soc_Request oLDomain = new CTGB_Soc_Request();
            //ComType:0322
            oLDomain.COM_TYPE = msgUtility.GetStr(msgBytes, "ReqType");

            oLDomain.READER_TYPE = msgUtility.GetStr(msgBytes, "ReaderType");
            
            oLDomain.READER_ID = msgUtility.GetStr(msgBytes, "ReaderId");

            oLDomain.MERC_FLG = msgUtility.GetStr(msgBytes, "MercFlg");

            oLDomain.STORE_NO = msgUtility.GetStr(msgBytes, "StoreNo");

            oLDomain.REG_ID = msgUtility.GetStr(msgBytes, "RegId");

            //RW 機種Code
            oLDomain.RW_TYPE = msgUtility.GetStr(msgBytes, "RwType");
            //端末區分
            oLDomain.READER_FLG = msgUtility.GetStr(msgBytes, "ReaderFlg");
            //Service Code: 總部Code 001
            oLDomain.SVC_FLG = msgUtility.GetStr(msgBytes, "SvcFlg");
            //機器種別
            oLDomain.MACHINE_TYPE = msgUtility.GetStr(msgBytes, "MachineType");
            //RW編號
            oLDomain.RW_NO = msgUtility.GetStr(msgBytes, "RwNo");
            
            //Data部分
            //特約機構代號
            oLDomain.MERCHANT_ID = msgUtility.GetStr(msgBytes, "MerchantID");
            //POS交易序號
            oLDomain.POS_SEQNO = msgUtility.GetStr(msgBytes, "PosSeqNo");
            //收銀員編號
            oLDomain.CASHIER_NO = msgUtility.GetStr(msgBytes, "CashierNo");
            //端末ID
            oLDomain.Terminal_ID = msgUtility.GetStr(msgBytes, "TerminalID");
            //前次加值日的加值水位餘額
            oLDomain.LOADING_BAL = msgUtility.GetStr(msgBytes, "LoadingBalance");
            //前次加值日的加值交易累計次數
            oLDomain.LOADING_SEQ = msgUtility.GetStr(msgBytes, "LoadingSequence");
            //電文產出日時
            oLDomain.DATETIME = msgUtility.GetStr(msgBytes, "DateTime");
            //request mac
            oLDomain.MAC = this.mac;
            log.Debug("4.結束轉換取PAM額度Request物件");
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
        protected virtual byte[] ParseResponse(IMsgUtility msgUtility, CTGB_Soc_Request response, byte[] request, bool doMAC = true)
        {
            log.Debug(m => m("7.轉換後台Response物件並建立MAC並轉成Response Byte[]"));
            byte[] rspResult = new byte[request.Length];
            Buffer.BlockCopy(request, 0, rspResult, 0, request.Length);//

            // modify request to response(必定回傳資訊)
            msgUtility.SetStr("02", rspResult, "Communicate");
            msgUtility.SetStr(response.RETURN_CODE, rspResult, "ReturnCode");
            msgUtility.SetStr(response.SN, rspResult, "CenterSeqNo");
            

            msgUtility.SetStr(this.transDateTime, rspResult, "DateTime");
            //是否產生mac(request的mac驗證成功就會還一個response,不論後台response正常或異常)
            if (doMAC)
            {
                msgUtility.SetStr(response.MERCHANT_ID, rspResult, "MerchantID");
                msgUtility.SetStr(response.STORE_NO, rspResult, "StoreID");
                msgUtility.SetStr(response.REG_ID, rspResult, "POSNO");
                msgUtility.SetStr(response.POS_SEQNO, rspResult, "PosSeqNo");
                msgUtility.SetStr(response.CASHIER_NO, rspResult, "CashierNo");
                msgUtility.SetStr(response.READER_ID, rspResult, "TerminalID");
                msgUtility.SetStr(response.LOADING_BAL, rspResult, "LoadingBalance");
                msgUtility.SetStr(response.LOADING_SEQ, rspResult, "LoadingSequence");
                
                // fetch sha1 data
                this.SetMAC(msgUtility, rspResult, this.readerId, this.transDateTime);
            }
            else
            {
                //若Mac驗證失敗,則使用來源mac簡易回傳
                msgUtility.SetStr(this.mac, rspResult, "Mac");
            }
            // padding data : SPEC length 0
            //this.SetPadding(msgUtility, rspResult);
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
        /// 送到後台的request物件並取回response物件
        /// </summary>
        /// <param name="request">request poco</param>
        /// <returns>response poco</returns>
        protected virtual CTGB_Soc_Request GetResponse(CTGB_Soc_Request request)
        {
            CTGB_Soc_Request result = null;
            HttpClient client = null;
            HttpRequestMessage requestMsg = null;
            string requestJSONstr = string.Empty;
            string jsonData = string.Empty;
            try
            {
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                requestJSONstr = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                log.Debug(m => m("5.[PAMQuota][Send] to Back-End Data: {0}", requestJSONstr));

                string url = ConfigLoader.GetSetting(ConType.PAMQuota);
                timer.Start();
                client = new HttpClient() { Timeout = new TimeSpan(0, 0, 0, 0, State_PAMQuota.ResponseTimeout) };
                requestMsg = new HttpRequestMessage(HttpMethod.Post, url);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                requestMsg.Content = new StringContent(requestJSONstr, Encoding.UTF8, "application/json");
                /*result = */
                client.SendAsync(requestMsg).ContinueWith(responseTask =>
                {
                    HttpResponseMessage response = responseTask.Result as HttpResponseMessage;
                    return response.Content.ReadAsByteArrayAsync().Result;
                }).ContinueWith(dataTask =>
                {
                    byte[] data = dataTask.Result as byte[];
                    jsonData = Encoding.UTF8.GetString(data);
                    //CTGB_Soc_Request tmp = null;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<CTGB_Soc_Request>(jsonData);
                    //return tmp;
                }).Wait(State_PAMQuota.ResponseTimeout);
                timer.Stop();
                log.Debug(m => m("6.[PAMQuota][Receive]Back-End Response(TimeSpend:{1}ms): {0}", jsonData, timer.ElapsedMilliseconds));


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
