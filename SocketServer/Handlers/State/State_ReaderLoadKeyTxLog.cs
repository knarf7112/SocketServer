using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//
using Common.Logging;
using ALCommon;
//using ;
using Newtonsoft.Json;
using DB_Module.Controller;
//
using Crypto.POCO;
using System.Net.Sockets;

namespace SocketServer.Handlers.State
{
    public class State_ReaderLoadKeyTxLog : IState
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(State_ReaderLoadKeyTxLog));
        public void Handle(AbsClientRequestHandler absClientRequestHandler)
        {
            #region variable
            byte[] receiveBuffer = null;
            int readCount = 0;
            string requestJsonStr = null;
            string outputCmd = null;
            
            EskmsKeyTxLogPOCO_v2 request = null;
            EskmsKeyTxLogPOCO_v2 response = null;
            string requestCheckErrMsg = null;
            string responseJsonStr = null;
            byte[] responseBytes = null;
            int sendCount = -1;
            AL_DBModule obDB = null;
            Shipment shipment = null;
            string txLogReturnCode = String.Empty;
            #endregion

            try
            {
                receiveBuffer = new byte[0x1000];//4k
                readCount = absClientRequestHandler.ClientSocket.Receive(receiveBuffer, SocketFlags.None);
                if (readCount == 0) { return; }
                //command 輸出狀態 TODO...
                else if (readCount == 6 && Encoding.UTF8.GetString(receiveBuffer, 0, readCount).ToLower().Contains("status"))
                {
                    outputCmd = "Hello";
                    receiveBuffer = Encoding.UTF8.GetBytes(outputCmd);
                    absClientRequestHandler.ClientSocket.Send(receiveBuffer);
                    return;
                }
                else
                {
                    log.Debug(m => m(">> {0}: {1}", this.GetType().Name, absClientRequestHandler.ClientNo));
                    //resize buffer
                    Array.Resize(ref receiveBuffer, readCount);
                    //init
                    obDB = new AL_DBModule();
                    shipment = new Shipment();

                    //casting jsonstring from buffer array
                    requestJsonStr = Encoding.UTF8.GetString(receiveBuffer);
                    log.Debug(m => m("[{0}]Request: {1}", this.GetType().Name, requestJsonStr));
                    request = JsonConvert.DeserializeObject<EskmsKeyTxLogPOCO_v2>(requestJsonStr);
                    //檢查Request資料長度(Attribute)
                    request.CheckLength(true, out requestCheckErrMsg);

                    //回應資料設定
                    response = new EskmsKeyTxLogPOCO_v2()
                    {
                        SAM_UID = request.SAM_UID,
                        DeviceId = request.DeviceId,
                        ReturnCode = "000001",//預設為fail
                    };

                    txLogReturnCode = request.TestTxLog.Substring(16, 8);//取第16~23個字串

                    //簡易判斷:TxLog ReturnCode
                    if (txLogReturnCode.Equals("00000000"))
                    {
                        log.Debug(m => m("1.Open DB Connection"));
                        obDB.OpenConnection();

                        log.Debug(m => m("2.Run Shipment DB Operate SAM_UID:{0}, DeviceId:{1}", request.SAM_UID, request.DeviceId));
                        //若有輸入顧客識別符號和卡機種別則寫入時就依據此資料寫入,若無則使用預設値寫入(在DB_Operate.SQL.Sql_Getter_Reader_D裡)
                        if (!String.IsNullOrEmpty(request.Merc_Flg) && !String.IsNullOrEmpty(request.Reader_Type))
                        {
                            log.Debug(m => m("2-1. 自訂卡機格式與顧客識別[Reader_Type:{0}, Merc_Flg:{1}]", request.Reader_Type, request.Merc_Flg));
                            //執行Reader出貨的DB操作流程
                            shipment.Shipment_Reader(obDB, request.SAM_UID, request.DeviceId, request.Reader_Type, request.Merc_Flg);//
                        }
                        else
                        {
                            log.Debug(m => m("2-2.預設的卡機格式與顧客識別[Reader_Type:{0}, Merc_Flg:{1}]", "03", "ICA"));
                            //執行Reader出貨的DB操作流程
                            shipment.Shipment_Reader(obDB, request.SAM_UID, request.DeviceId);//
                        }
                        response.ReturnCode = "000000";
                        log.Debug(m => m("3.Shipment DB Operate End ..."));
                    }
                    else
                    {
                        log.Error(m => m("UID:{0}  DeviceId:{1}  TxLog ReturnCode異常:{2}",request.SAM_UID, request.DeviceId, txLogReturnCode));
                    }
                    responseJsonStr = JsonConvert.SerializeObject(response);
                    responseBytes = Encoding.UTF8.GetBytes(responseJsonStr);
                    log.Debug(m => m("[{0}] Response:{1}", this.GetType().Name, responseJsonStr));
                    sendCount = absClientRequestHandler.ClientSocket.Send(responseBytes);
                    if (sendCount != responseBytes.Length)
                    {
                        log.Error(m => m("異常:送出資料(length:{0}不等於原始資料(length:{1}))", sendCount, responseBytes.Length));
                    }
                    log.Debug(m => m("[{0}] Response End", this.GetType().Name));
                }

            }
            catch (ArgumentOutOfRangeException ex)
            {
                log.Error(m => m("資料檢核失敗:{0}", ex.ToString()));
            }
            catch (JsonException ex)
            {
                log.Error(m => m("Request(JsonString) Parse Request(Object) Failed:{0}", ex.ToString()));
            }
            catch (Exception ex)
            {
                log.Error(m => m("[{0}] Error:{1} {2}", this.GetType().Name, ex.Message, ex.StackTrace));
            }
            finally
            {
                if (obDB != null && obDB.ALCon != null)
                {
                    obDB.CloseConnection();
                    shipment = null;
                }
                absClientRequestHandler.ServiceState = new State_Exit();
            }
        }
    }
}
