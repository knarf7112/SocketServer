using System;
using System.Text;
//POCO
using ALCommon;
using Common.Logging;
using System.Net.Sockets;
using Kms2.Crypto.Utility;

namespace SocketServer.v2.Handlers.State
{
    /// <summary>
    /// 處理狀態:自動加值TxLog
    /// </summary>
    public class State_AutoLoadTxLog : IState
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(State_AutoLoadTxLog));
        public void Handle(ClientRequestHandler handler)
        {
            try
            {
                ALTxlog_Domain request = null;
                ALTxlog_Domain response = null;
                byte[] responseArr = null;
                log.Debug(m => m("[State_AutoLoadTxLog] Request(ASCII):{0}", ClientRequestHandler.asciiOctets2String(handler.Request)));
                try
                {
                    //1.parse Txlog
                    request = this.ParseRequest(CheckMacContainer.TOLReqMsgUtility,handler.Request);

                    //2.send to Back-End request and get response
                    response = this.GetResponse(request);
                    if (response == null || String.IsNullOrEmpty(response.TXLOG_RC))
                    {
                        //後台 error
                        response = new ALTxlog_Domain()
                        {
                            TXLOG_RC = "990001"
                        };
                    }
                }
                catch(Exception ex)
                {
                    log.Error(m => m("[State_AutoLoadTxLog][Handle] Business Error: {0} \r\n{1}", ex.Message, ex.StackTrace));
                    //格式 error
                    response = new ALTxlog_Domain()
                    {
                        TXLOG_RC = "770001"
                    };
                }
                //3.開始轉換Response
                responseArr = this.ParseResponse(CheckMacContainer.TOLRespMsgUtility,response, handler.Request);
                log.Info(m => m("Response(length:{0}):{1}", responseArr.Length, BitConverter.ToString(responseArr).Replace("-", "")));
                int sendLength = handler.ClientSocket.Send(responseArr);
                if (sendLength != responseArr.Length)
                {
                    log.Error(m => m("Response[length:{0}] not equal Actual Send Response[Length:{1}]", responseArr.Length, sendLength));
                }
            }
            catch (SocketException sckEx)
            {
                log.Error(m => m("[State_AutoLoadTxLog][Handle] Socket Error: {0} \r\n{1}", sckEx.Message, sckEx.StackTrace));
            }
            catch (Exception ex)
            {
                log.Error(m => m("[State_AutoLoadTxLog][Handle] Error: {0} \r\n{1}", ex.Message, ex.StackTrace));
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
        protected virtual ALTxlog_Domain GetResponse(ALTxlog_Domain request)
        {
            ALTxlog_Domain result = null;
            SocketClient.Domain.SocketClient socketClient = null;
            try
            {
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                SocketClient.Domain.Utilities.NewJsonWorker<ALTxlog_Domain> jsonWorker = new SocketClient.Domain.Utilities.NewJsonWorker<ALTxlog_Domain>();
                byte[] dataByte = jsonWorker.Serialize2Bytes(request);
                log.Debug(m => m("3.[AutoLoadTxLog][Send] to Back-End Data: {0}", Encoding.ASCII.GetString(dataByte)));
                string[] setting = ConfigLoader.GetSetting(ConType.AutoLoadTxLog).Split(':');
                string ip = setting[0];
                int port = Convert.ToInt32(setting[1]);
                int sendTimeout = Convert.ToInt32(setting[2]);
                int receiveTimeout = Convert.ToInt32(setting[3]);
                timer.Start();
                socketClient = new SocketClient.Domain.SocketClient(ip, port, sendTimeout, receiveTimeout);
                if (socketClient.ConnectToServer())
                {
                    byte[] resultBytes = null;
                    resultBytes = socketClient.SendAndReceive(dataByte);
                    timer.Stop();
                    log.Debug(m => m("4.[AutoLoadTxLog][Receive]Back-End Response(TimeSpend:{1}ms): {0}", Encoding.ASCII.GetString(resultBytes), timer.ElapsedMilliseconds));
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

        /// <summary>
        /// Request截取需要的資料轉成POCO
        /// </summary>
        /// <param name="msgUtility">Msg Parser Object</param>
        /// <param name="msgBytes">Origin Request byte array</param>
        /// <returns>POCO</returns>
        protected virtual ALTxlog_Domain ParseRequest(IMsgUtility msgUtility,byte[] msgBytes)
        {
            log.Debug("1.開始轉換自動加值TxLog Request物件");
            ALTxlog_Domain oLDomain = new ALTxlog_Domain();
            //ComType:0333
            oLDomain.COM_TYPE = msgUtility.GetStr(msgBytes, "ReqType");

            oLDomain.POS_FLG = msgUtility.GetStr(msgBytes, "ReaderType");

            oLDomain.READER_ID = msgUtility.GetStr(msgBytes, "ReaderId").Substring(0, 16);

            oLDomain.MERC_FLG = msgUtility.GetStr(msgBytes, "MercFlg");

            oLDomain.STORE_NO = msgUtility.GetStr(msgBytes, "StoreNo");

            oLDomain.REG_ID = msgUtility.GetStr(msgBytes, "RegId");
            if("SET".Equals(oLDomain.MERC_FLG))
            {
                oLDomain.STORE_NO =oLDomain.STORE_NO.Substring(2, 6);
                oLDomain.REG_ID = oLDomain.REG_ID.Substring(1, 2);
            }
            //
            oLDomain.SN = msgUtility.GetStr(msgBytes, "CenterSeqNo");
            //
            oLDomain.POS_SEQNO = msgUtility.GetStr(msgBytes, "PosSeqNo");
            //
            oLDomain.TXLOG = msgUtility.GetStr(msgBytes, "CadLog");

            log.Debug("2.結束轉換自動加值TxLog Request物件");
            return oLDomain;
        }

        /// <summary>
        /// Response物件轉換成byte[]
        /// </summary>
        /// <param name="msgUtility">Msg Parser Object</param>
        /// <param name="response">後端給的POCO</param>
        /// <param name="request">Origin Request byte array</param>
        /// <returns>response byte array</returns>
        protected virtual byte[] ParseResponse(IMsgUtility msgUtility,ALTxlog_Domain response, byte[] request)
        {
            log.Debug(m => m("5.轉換後台自動加值TxLog Response物件 => Byte[]"));
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
            log.Debug(m => m("6.轉換後台自動加值TxLog Response物件完畢"));
            return rspResult;
        }
    }
}
