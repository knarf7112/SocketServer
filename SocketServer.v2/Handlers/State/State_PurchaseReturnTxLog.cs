using System;
using System.Text;
//
using Kms2.Crypto.Utility;
//Log
using Common.Logging;
//POCO
using LOLCommon;
using System.Net.Http;
//

namespace SocketServer.v2.Handlers.State
{
    /// <summary>
    /// 處理狀態:購貨取消TxLog
    /// </summary>
    public class State_PurchaseReturnTxLog : IState
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(State_PurchaseReturnTxLog));
        /// <summary>
        /// Http Response TimeOut
        /// </summary>
        private static readonly int ResponseTimeout = 30000;
        public void Handle(ClientRequestHandler handler)
        {
            try
            {
                Txlog_Domain request = null;
                Txlog_Domain response = null;
                byte[] responseArr = null;
                log.Debug(m => m("[State_PurchaseReturnTxLog] {0} Request(ASCII):{1}", handler.ClientSocket.RemoteEndPoint.ToString(), ClientRequestHandler.asciiOctets2String(handler.Request)));
                try
                {
                    //1.parse Txlog
                    request = this.ParseRequest(CheckMacContainer.PRTxLogReqMsgUtility, handler.Request);

                    //2.send to Back-End request and get response
                    response = this.GetResponse(request);
                    if (response == null || String.IsNullOrEmpty(response.TXLOG_RC))
                    {
                        //後台 error
                        response = new Txlog_Domain()
                        {
                            TXLOG_RC = "990001"
                        };
                    }
                }
                catch (Exception ex)
                {
                    log.Error(m => m("[State_PurchaseReturnTxLog][Handle] Business Error: {0} \r\n{1}", ex.Message, ex.StackTrace));
                    //格式 error
                    response = new Txlog_Domain()
                    {
                        TXLOG_RC = "770001"
                    };
                }
                //3.開始轉換Response
                responseArr = this.ParseResponse(CheckMacContainer.PRTxLogRespMsgUtility, response, handler.Request);
                log.Info(m => m("Response(length:{0}):{1}", responseArr.Length, BitConverter.ToString(responseArr).Replace("-", "")));
                int sendLength = handler.ClientSocket.Send(responseArr);
                if (sendLength != responseArr.Length)
                {
                    log.Error(m => m("Response[length:{0}] not equal Actual Send Response[Length:{1}]", responseArr.Length, sendLength));
                }
            }
            catch (Exception ex)
            {
                log.Error(m => m("[State_PurchaseReturnTxLog][Handle] Error: {0} \r\n{1}", ex.Message, ex.StackTrace));
            }
            finally
            {
                handler.ServiceState = new State_Exit();
            }
        }

        /// <summary>
        /// 送出Txlog request到後台並取回 Txlog response
        /// </summary>
        /// <param name="request">要送後台的Txlog物件</param>
        /// <returns>後來回來的Txlog物件</returns>
        protected virtual Txlog_Domain GetResponse(Txlog_Domain request)
        {
            Txlog_Domain result = null;
            HttpClient client = null;
            HttpRequestMessage requestMsg = null;
            string requestJSONstr = string.Empty;
            string jsonData = string.Empty;
            try
            {
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                requestJSONstr = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                log.Debug(m => m("3.[PurchaseReturnTxLog][Send] to Back-End Data: {0}", requestJSONstr));

                string url = ConfigLoader.GetSetting(ConType.PurchaseReturnTxLog);
                timer.Start();
                client = new HttpClient() { Timeout = new TimeSpan(0, 0, 0, 0, State_PurchaseReturnTxLog.ResponseTimeout) };
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
                    try
                    {
                        result = Newtonsoft.Json.JsonConvert.DeserializeObject<Txlog_Domain>(jsonData);
                    }
                    catch (Exception ex)
                    {
                        log.Error(m => m("Response轉換失敗:{0} \r\n{1}", ex.Message, jsonData));
                    }
                }).Wait(State_PurchaseReturnTxLog.ResponseTimeout);
                timer.Stop();
                log.Debug(m => m("4.[PurchaseReturnTxLog][Receive]Back-End Response(TimeSpend:{1}ms): {0}", jsonData, timer.ElapsedMilliseconds));


                return result;
            }
            catch (Exception ex)
            {
                log.Error("[GetResponse]Send Back-End Error: " + ex.Message + " \r\n" + ex.StackTrace);
                return null;
            }

        }

        /// <summary>
        /// Request截取需要的資料轉成POCO
        /// </summary>
        /// <param name="msgUtility">Msg Parser Object</param>
        /// <param name="msgBytes">Origin Request byte array</param>
        /// <returns>POCO</returns>
        protected virtual Txlog_Domain ParseRequest(IMsgUtility msgUtility, byte[] msgBytes)
        {
            /*
             {"COM_TYPE":"0341","MERCHANT_NO":null,"MERC_FLG":"SET","M_KIND":null,"POS_FLG":"01","POS_SEQNO":"471756","READER_ID":"8604241F6A9F2980    ","REG_ID":"01","SN":"02922396","STORE_NO":"113584","TRANS_TYPE":null,"TXLOG":"52201411031908470000000073111400240070060000020004241F6A9F2980000005000000420000001500009100000000270000880604241F6A9F298000000000000000000000000100000200000000006520C0FBSET00100113584100118604241F6A9F2980    00000294000000000000000000000000000000000000000000000000000000000000000000000A2","TXLOG_RC":null}
             */
            log.Debug("1.開始轉換購貨取消TxLog Request物件");
            Txlog_Domain oLDomain = new Txlog_Domain();
            //ComType:0333
            oLDomain.COM_TYPE = msgUtility.GetStr(msgBytes, "ReqType");

            oLDomain.POS_FLG = msgUtility.GetStr(msgBytes, "ReaderType");

            oLDomain.READER_ID = msgUtility.GetStr(msgBytes, "ReaderId").Substring(0, 16);

            oLDomain.MERC_FLG = msgUtility.GetStr(msgBytes, "MercFlg");

            oLDomain.STORE_NO = msgUtility.GetStr(msgBytes, "StoreNo");

            oLDomain.REG_ID = msgUtility.GetStr(msgBytes, "RegId");
            if ("SET".Equals(oLDomain.MERC_FLG))
            {
                oLDomain.STORE_NO = oLDomain.STORE_NO.Substring(2, 6);
                oLDomain.REG_ID = oLDomain.REG_ID.Substring(1, 2);
            }
            //
            oLDomain.SN = msgUtility.GetStr(msgBytes, "CenterSeqNo");
            //
            oLDomain.POS_SEQNO = msgUtility.GetStr(msgBytes, "PosSeqNo");
            //
            oLDomain.TXLOG = msgUtility.GetStr(msgBytes, "CadLog");

            log.Debug("2.結束轉換購貨取消TxLog Request物件");
            return oLDomain;
        }

        /// <summary>
        /// Response物件轉換成byte[]
        /// </summary>
        /// <param name="msgUtility">Response Msg Parser</param>
        /// <param name="response">後端給的POCO</param>
        /// <param name="request">Origin Request byte array</param>
        /// <returns>response byte array</returns>
        protected virtual byte[] ParseResponse(IMsgUtility msgUtility, Txlog_Domain response, byte[] request)
        {
            log.Debug(m => m("5.轉換後台購貨取消TxLog Response物件 => Byte[]"));
            byte[] rspResult = new byte[request.Length];
            Buffer.BlockCopy(request, 0, rspResult, 0, request.Length);//
            // modify request to response
            msgUtility.SetStr("02", rspResult, "Communicate");
            msgUtility.SetStr(response.TXLOG_RC, rspResult, "ReturnCode");
            //取得資料部分的大小並設定
            string sizeStr = CheckMacContainer.StrHelper.LeftPad(msgUtility.GetSize("DataVersion", "EndOfData"), msgUtility.GetTag("DecryptSize").Length, '0');
            msgUtility.SetStr(sizeStr, rspResult, "DecryptSize");
            msgUtility.SetStr(sizeStr, rspResult, "EncryptSize");
            //寫入空白
            msgUtility.SetBytes(CheckMacContainer.ByteWorker.Fill(msgUtility.GetTag("DataPadding").Length, 0x20), rspResult, "DataPadding");
            //依據定義改變Response大小(因Request資料部分長度與Response資料部分長度不一樣)
            rspResult = CheckMacContainer.ByteWorker.SubArray(rspResult, 0, msgUtility.GetSize("HeaderVersion", "EndOfData"));
            log.Debug(m => m("6.轉換後台購貨取消TxLog Response物件完畢"));
            return rspResult;
        }
    }
}
