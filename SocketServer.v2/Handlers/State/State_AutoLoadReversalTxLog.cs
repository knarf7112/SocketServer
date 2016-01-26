using System;
using System.Text;
//POCO
using ALCommon;
//Socket
using System.Net.Sockets;
//Log
using Common.Logging;

namespace SocketServer.v2.Handlers.State
{
    /// <summary>
    /// 處理狀態:自動加值沖正TxLog
    /// </summary>
    public class State_AutoLoadReversalTxLog : State_AutoLoadTxLog
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(State_AutoLoadReversalTxLog));
        /// <summary>
        /// 送出沖正Txlog request到後台並取回 沖正Txlog response
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected override ALTxlog_Domain GetResponse(ALTxlog_Domain request)
        {
            ALTxlog_Domain result = null;
            SocketClient.Domain.SocketClient socketClient = null;
            try
            {
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                SocketClient.Domain.Utilities.NewJsonWorker<ALTxlog_Domain> jsonWorker = new SocketClient.Domain.Utilities.NewJsonWorker<ALTxlog_Domain>();
                byte[] dataByte = jsonWorker.Serialize2Bytes(request);
                log.Debug(m => m("3.[AutoLoadReversalTxLog][Send] to Back-End Data: {0}", Encoding.ASCII.GetString(dataByte)));
                string[] setting = ConfigLoader.GetSetting(ConType.AutoLoadReversalTxLog).Split(':');
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
                    log.Debug(m => m("4.[AutoLoadReversalTxLog][Receive]Back-End Response(TimeSpend:{1}ms): {0}", Encoding.ASCII.GetString(resultBytes), timer.ElapsedMilliseconds));
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
