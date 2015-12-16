using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//
using Common.Logging;
using Newtonsoft.Json;
using Crypto.POCO;
using Crypto.EskmsAPI;
using System.Net.Sockets;
//DB 
using DB_Module;
using ALCommon;
using DB_Module.Controller;

namespace SocketServer.Handlers.State
{
    /// <summary>
    /// 驗證ReaderId並取得Divers key
    /// </summary>
    public class State_ReaderLoadKey : IState
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(State_ReaderLoadKey));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="absClientRequestHandler"></param>
        public void Handle(AbsClientRequestHandler absClientRequestHandler)
        {
            #region variable
            byte[] receiveBuffer = null;
            int readCount = 0;
            string requestJsonStr = null;
            string outputCmd = null;
            IKMSGetter kmsGetter = null;
            EskmsKeyPOCO request = null;
            EskmsKeyPOCO response = null;
            string requestCheckErrMsg = null;
            string responseJsonStr = null;
            byte[] responseBytes = null;
            byte[] diversKey = null;
            int sendCount = -1;
            AL_DBModule obDB = null;
            Shipment shipment = null;
            bool hasReaderId = false;
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
                    request = JsonConvert.DeserializeObject<EskmsKeyPOCO>(requestJsonStr);
                    
                    //檢查Request資料長度(Attribute)
                    request.CheckLength(true, out requestCheckErrMsg);
                    
                    log.Debug(m => m("1.Open DB Connection"));
                    obDB.OpenConnection();
                    
                    log.Debug(m => m("2.Run DB Command"));
                    hasReaderId = shipment.Check_ReaderId_FromSAM_D(obDB, request.Input_UID, "21");
                    
                    //設定Authenticate參數
                    kmsGetter = new KMSGetter()
                    {
                        Input_KeyLabel = request.Input_KeyLabel,
                        Input_KeyVersion = request.Input_KeyVersion,
                        Input_UID = request.Input_UID,
                        Input_DeviceID = request.Input_DeviceID
                    };

                    if (hasReaderId)
                    {
                        log.Debug(m => m("3.開始取Divers Key"));
                        diversKey = kmsGetter.GetDiversKey();//會傳送數據到KMS並取回DiverseKey後做運算並將結果寫入Output屬性中
                    }
                    //回應資料設定
                    response = new EskmsKeyPOCO()
                    {
                        Input_KeyLabel = request.Input_KeyLabel,
                        Input_KeyVersion = request.Input_KeyVersion,
                        Input_UID = request.Input_UID,
                        Output_DiversKey = diversKey
                    };
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
            catch (System.Data.SqlClient.SqlException sqlEx)
            {
                log.Error(m => m("DB Operate Failed:{0}", sqlEx.ToString()));
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
                if (obDB != null)
                {
                    obDB.CloseConnection();
                    shipment = null;
                }
                absClientRequestHandler.ServiceState = new State_Exit();
            }
        }
    }
}
