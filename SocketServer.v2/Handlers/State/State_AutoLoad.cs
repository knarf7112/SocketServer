using System;
using System.Text;
//
using Common.Logging;
//ALOL POCO
using ALCommon;
//socket client
using SocketClient.Domain.Utilities;
using System.Net.Sockets;
using Kms2.Crypto.Utility;

namespace SocketServer.v2.Handlers.State
{
    /// <summary>
    /// 處理狀態:自動加值
    /// </summary>
    public class State_AutoLoad : IState
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(State_AutoLoad));
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
                AL2POS_Domain request = null;
                bool doMac = true;
                AL2POS_Domain response = null;
                byte[] responseArr = null;
                log.Debug(m => m("[State_AutoLoad] {0} Request(ASCII):{1}", handler.ClientSocket.RemoteEndPoint.ToString(), ClientRequestHandler.asciiOctets2String(handler.Request)));
                //1.驗證Mac
                if (this.CheckMac(CheckMacContainer.AOLReqMsgUtility,handler.Request))
                {
                    //2.parse request
                    request = this.ParseRequest(CheckMacContainer.AOLReqMsgUtility,handler.Request);
                    //3.send to Back-End request and get response
                    response = this.GetResponse(request);
                    if (response == null)
                    {
                        //back-End Error
                        response = new AL2POS_Domain()
                        {
                            AL2POS_SN = "99999999",
                            AL2POS_RC = "990001"
                        };
                    }
                }
                else
                {
                    //MAC驗證失敗
                    doMac = false;
                    response = new AL2POS_Domain()
                    {
                        AL2POS_SN = "99999999",
                        AL2POS_RC = "990007"
                    };
                }

                //4. parse response byte array(create mac)
                responseArr = this.ParseResponse(CheckMacContainer.AOLRespMsgUtility,response, handler.Request, doMac);

                log.Info(m => m("Response(length:{0}):{1}",responseArr.Length, BitConverter.ToString(responseArr).Replace("-", "")));
                int sendLength = handler.ClientSocket.Send(responseArr);
                if (sendLength != responseArr.Length)
                {
                    log.Error(m => m("Response[length:{0}] not equal Actual Send Response[Length:{1}]", responseArr.Length, sendLength));
                }
            }
            catch (SocketException sckEx)
            {
                log.Error(m => m("[State_AutoLoad][Handle] Socket Error: {0} \r\n{1}", sckEx.Message, sckEx.StackTrace));
            }
            catch (Exception ex)
            {
                log.Error(m => m("[State_AutoLoad][Handle] Error: {0} \r\n{1}", ex.Message, ex.StackTrace));
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
        protected virtual bool CheckMac(IMsgUtility msgUtility,byte[] msgBytes)
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
                log.Error(m => m("[State_AutoLoad][CheckMac] Error: {0} \r\n{1}", ex.Message, ex.StackTrace));
                return false;
            }
        }

        /// <summary>
        /// Request依據規格書截取需要的資料轉成POCO
        /// </summary>
        /// <param name="msgUtility">Request Msg Parser</param>
        /// <param name="msgBytes">Origin Request byte array</param>
        /// <returns>POCO</returns>
        protected virtual AL2POS_Domain ParseRequest(IMsgUtility msgUtility,byte[] msgBytes)
        {
            log.Debug("3.開始轉換自動加值Request物件");
            AL2POS_Domain oLDomain = new AL2POS_Domain();
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
            oLDomain.AL_AMT = Convert.ToInt32(this.amount);
            //
            oLDomain.ICC_NO = msgUtility.GetStr(msgBytes, "CardNo");

            oLDomain.AL_TRANSTIME = this.transDateTime;//CheckMacContainer.AOLReqMsgUtility.GetStr(msgBytes, "TransDateTime");
            //
            oLDomain.AL_ENABLE = msgUtility.GetStr(msgBytes, "AutoloadEnable");
            log.Debug("4.結束轉換自動加值Request物件");
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
        protected virtual byte[] ParseResponse(IMsgUtility msgUtility,AL2POS_Domain response,byte[] request,bool doMAC = true)
        {
            log.Debug(m => m("7.轉換後台Response物件並建立MAC並轉成Response Byte[]"));
            byte[] rspResult = new byte[request.Length];
            Buffer.BlockCopy(request, 0, rspResult, 0, request.Length);//

            
            // modify request to response
            msgUtility.SetStr("02", rspResult, "Communicate");
            msgUtility.SetStr(response.AL2POS_RC, rspResult, "ReturnCode");
            msgUtility.SetStr(response.AL2POS_SN, rspResult, "CenterSeqNo");
            msgUtility.SetStr(this.amount, rspResult, "Amount");

            msgUtility.SetStr(this.transDateTime, rspResult, "TransDateTime");
            //是否產生mac
            if (doMAC)
            {
                // fetch sha1 data
                this.SetMAC(msgUtility, rspResult, this.readerId, this.transDateTime);//CheckMacContainer.Mac2Manager.GetAuthMac(response.READER_ID, response.AL_TRANSTIME, sha1Result);
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
        protected virtual void SetMAC(IMsgUtility resMsgUility,byte[] response,string reader_id,string transTime)
        {
            // fetch sha1 data
            byte[] hashDataBytes = resMsgUility.GetBytes(response, "HeaderVersion", "Mac");
            byte[] sha1Result = CheckMacContainer.HashWorker.ComputeHash(hashDataBytes);
            string mac = CheckMacContainer.Mac2Manager.GetAuthMac(reader_id, transTime, sha1Result);
            log.Debug(m => m("8.Response mac: {0}", mac));

            resMsgUility.SetStr(mac, response, "Mac");
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
            //log.Debug(m => m("9.Response Byte[]轉換完成, length:{0}", response.Length));
            int length  = response.Length;
            log.Debug(m => m("9.Response Padding End (array length:{0})", length));
        }

        /// <summary>
        /// 送到後台的request物件並取回response物件
        /// </summary>
        /// <param name="request">request poco</param>
        /// <returns>response poco</returns>
        protected virtual AL2POS_Domain GetResponse(AL2POS_Domain request)
        {
            AL2POS_Domain result = null;
            SocketClient.Domain.SocketClient socketClient = null;
            try
            {
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                byte[] resultBytes = null;
                NewJsonWorker<AL2POS_Domain> jsonWorker = new NewJsonWorker<AL2POS_Domain>();
                byte[] dataByte = jsonWorker.Serialize2Bytes(request);
                log.Debug(m => m("5.[AutoLoad][Send] to Back-End Data: {0}", Encoding.ASCII.GetString(dataByte)));
                string[] setting = ConfigLoader.GetSetting(ConType.AutoLoad).Split(':');
                string ip = setting[0];
                int port = Convert.ToInt32(setting[1]);
                int sendTimeout = Convert.ToInt32(setting[2]);
                int receiveTimeout = Convert.ToInt32(setting[3]);
                timer.Start(); 
                socketClient = new SocketClient.Domain.SocketClient(ip, port, sendTimeout, receiveTimeout);
                if (socketClient.ConnectToServer())
                {
                    resultBytes = socketClient.SendAndReceive(dataByte);
                    timer.Stop();
                    log.Debug(m => m("6.[AutoLoad][Receive]Back-End Response(TimeSpend:{1}ms): {0}", Encoding.ASCII.GetString(resultBytes), timer.ElapsedMilliseconds));
                    result = jsonWorker.Deserialize(resultBytes);
                }
                return result;
            }
            catch (SocketException sckEx)
            {
                log.Error("[GetResponse]Send Back-End Socket Error: " + sckEx.Message + " \r\n" + sckEx.StackTrace);
                return null;
            }
            catch (Exception ex)
            {
                log.Error("[GetResponse]Send Back-End Error: " + ex.Message + " \r\n" + ex.StackTrace);
                return null;
            }
            finally
            {
                if (socketClient != null)
                {
                    socketClient.CloseConnection();
                }
            }
        }
    }
}
