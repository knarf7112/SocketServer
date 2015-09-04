using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//
using Common.Logging;
using System.Net.Sockets;
using Crypto.EskmsAPI;
using Crypto.POCO;
using Crypto;
using Newtonsoft.Json;
using System.Diagnostics;

namespace SocketServer.Handlers.State
{
    /// <summary>
    /// 
    /// </summary>
    public class State_Authenticate : IState
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(State_Authenticate));

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
            iBonAuthenticate iBonAuthObj = null;
            EskmsPOCO request = null;
            EskmsPOCO response = null;
            Stopwatch timer = null;
            string requestCheckErrMsg = null;
            string responseJsonStr = null;
            byte[] responseBytes = null;
            int sendCount = -1;
            #endregion

            try
            {
                receiveBuffer = new byte[0x1000];//4k
                readCount = absClientRequestHandler.ClientSocket.Receive(receiveBuffer, SocketFlags.None);
                if (readCount == 0) { return; }
                    //command 輸出狀態 TODO...
                else if (readCount == 11 && Encoding.UTF8.GetString(receiveBuffer, 0, readCount).ToLower().Contains("status"))
                {
                    outputCmd = "Hello";
                    receiveBuffer = Encoding.UTF8.GetBytes(outputCmd);
                    absClientRequestHandler.ClientSocket.Send(receiveBuffer);
                    return;
                }
                else
                {
                    log.Debug(m => m(">> {0}: {1}", this.GetType().Name, absClientRequestHandler.ClientNo));
                    timer = new Stopwatch();
                    //resize buffer
                    Array.Resize(ref receiveBuffer, readCount);
                    //casting jsonstring from buffer array
                    requestJsonStr = Encoding.UTF8.GetString(receiveBuffer);
                    log.Debug(m => m("[{0}]Request: {1}", this.GetType().Name, requestJsonStr));
                    request = JsonConvert.DeserializeObject<EskmsPOCO>(requestJsonStr);
                    //檢查Request資料長度

                    request.CHeckLength(true, out requestCheckErrMsg);
                    //設定Authenticate參數
                    iBonAuthObj = new iBonAuthenticate()
                    {
                        Input_KeyLabel = request.Input_KeyLabel,
                        Input_KeyVersion = request.Input_KeyVersion,
                        Input_UID = request.Input_UID,
                        Input_Enc_RanB = request.Input_Enc_RanB
                    };
                    log.Debug(m => m("開始執行Authenticate"));
                    iBonAuthObj.StartAuthenticate(true);


                    response = new EskmsPOCO()
                    {
                        Input_KeyLabel = request.Input_KeyLabel,
                        Input_KeyVersion = request.Input_KeyVersion,
                        Input_UID = request.Input_UID,
                        Input_Enc_RanB = request.Input_Enc_RanB,
                        Output_RanB = iBonAuthObj.Output_RanB,
                        Output_Enc_RanAandRanBRol8 = iBonAuthObj.Output_Enc_RanAandRanBRol8,
                        Output_Enc_IVandRanARol8 = iBonAuthObj.Output_Enc_IVandRanARol8,
                        Output_RandAStartIndex = iBonAuthObj.Output_RandAStartIndex,
                        Output_SessionKey= iBonAuthObj.Output_SessionKey
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
            catch (Exception ex)
            {
                log.Error(m => m("[{0}] Error:{1} {2}", this.GetType().Name, ex.Message, ex.StackTrace));
            }
            finally
            {

                absClientRequestHandler.ServiceState = new State_Exit();
            }
        }
    }
}
