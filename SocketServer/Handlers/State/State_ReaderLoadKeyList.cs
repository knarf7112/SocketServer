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
using System.Diagnostics;

namespace SocketServer.Handlers.State
{
    class State_ReaderLoadKeyList : IState
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(State_ReaderLoadKeyList));

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
            IList<EskmsKeyPOCO> request_list = null;
            IList<EskmsKeyPOCO> response_list = null;
            EskmsKeyPOCO response = null;
            IList<string> UID_Failed_List;
            Stopwatch timer;
            //string requestCheckErrMsg = null;
            string responseJsonStr = null;
            string failed_list = null;
            byte[] responseBytes = null;
            byte[] diversKey = null;
            int sendCount = -1;
            AL_DBModule obDB = null;
            Shipment shipment = null;
            bool hasReaderId = false;
            #endregion

            try
            {
                receiveBuffer = new byte[0x4000];//4k
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
                    UID_Failed_List = new List<string>();
                    response_list = new List<EskmsKeyPOCO>();
                    timer = new Stopwatch();
                    timer.Start();
                    //casting jsonstring from buffer array
                    requestJsonStr = Encoding.UTF8.GetString(receiveBuffer);
                    log.Debug(m => m("[{0}]Request: {1}", this.GetType().Name, requestJsonStr));
                    request_list = JsonConvert.DeserializeObject<IList<EskmsKeyPOCO>>(requestJsonStr);

                    //檢查Request資料長度(Attribute) //不能檢查  因為沒有DeviceID
                    //poco.CheckLength(true, out requestCheckErrMsg);
                    log.Debug(m => m("1.Open DB Connection"));
                    obDB.OpenConnection();
                    log.Debug(m => m("2.Run DB Command"));
                    foreach (EskmsKeyPOCO request in request_list)
                    {
                        hasReaderId = shipment.Check_ReaderId_FromSAM_D(obDB, request.Input_UID, "21");
                        if (hasReaderId)
                        {
                            //設定Authenticate參數
                            kmsGetter = new KMSGetter()
                            {
                                Input_KeyLabel = request.Input_KeyLabel,
                                Input_KeyVersion = request.Input_KeyVersion,
                                Input_UID = request.Input_UID,
                                Input_DeviceID = request.Input_DeviceID
                            };

                            log.Debug(m => m("3.開始取Divers Key"));
                            diversKey = kmsGetter.GetDiversKey();//會傳送數據到KMS並取回DiverseKey後做運算並將結果寫入Output屬性中
                            if (diversKey == null)
                            {
                                UID_Failed_List.Add(request.Input_UID);
                            }
                            else
                            {
                                //回應資料設定
                                response = new EskmsKeyPOCO()
                                {
                                    Input_KeyLabel = request.Input_KeyLabel,
                                    Input_KeyVersion = request.Input_KeyVersion,
                                    Input_UID = request.Input_UID,
                                    Output_DiversKey = diversKey
                                };
                                response_list.Add(response);
                            }
                        }
                        else
                        {
                            UID_Failed_List.Add(request.Input_UID);
                        }
                    }
                    if (UID_Failed_List.Count > 0)
                    {
                        failed_list = JsonConvert.SerializeObject(UID_Failed_List);
                        log.Debug(m => m("[{0}] 取Key失敗的UID列表:{1}", this.GetType().Name, failed_list));
                    }
                    responseJsonStr = JsonConvert.SerializeObject(response_list);
                    responseBytes = Encoding.UTF8.GetBytes(responseJsonStr);
                    log.Debug(m => m("[{0}] Response:{1}", this.GetType().Name, responseJsonStr));
                    sendCount = absClientRequestHandler.ClientSocket.Send(responseBytes);
                    if (sendCount != responseBytes.Length)
                    {
                        log.Error(m => m("異常:送出資料(length:{0}不等於原始資料(length:{1}))", sendCount, responseBytes.Length));
                    }
                    timer.Stop();
                    log.Debug(m => m("[{0}] Response End TimeSpend:{1}ms", this.GetType().Name, timer.ElapsedMilliseconds));
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
