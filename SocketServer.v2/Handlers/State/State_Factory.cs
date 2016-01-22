using System;
using System.ComponentModel;
using System.Text;
//log
//using Common.Logging;

namespace SocketServer.v2.Handlers.State
{
    /// <summary>
    /// 處理狀態: 依據資料Command選擇處理狀態
    /// </summary>
    public class State_Factory : IState
    {
        //private static readonly ILog log = LogManager.GetLogger(typeof(State_Factory));
        private byte[] receiveData { get; set; }

        /// <summary>
        /// 注入資料
        /// </summary>
        /// <param name="receiveData">注入的資料</param>
        public State_Factory(byte[] receiveData)
        {
            this.receiveData = receiveData;
        }
        /// <summary>
        /// 狀態選擇
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public IState StateSelect(ClientRequestHandler handler)
        {
            string conType = string.Empty;
            handler.Request = this.receiveData;// Encoding.ASCII.GetString(this.receiveData);
            //Console.WriteLine("State Factory Select Request");
            if (handler.Request.Length > 10)
            {
                //依據接收字串內的某個Command字串
                conType = Encoding.ASCII.GetString(handler.Request).Substring(6, 4);//0332,0333,0334,0531,0331,0341,0631,0641
                //選擇處理狀態
                switch (conType)
                {
                        //自動加値類
                    case "0332":
                        return new State_AutoLoad();
                    case "0333":
                        return new State_AutoLoadTxLog();
                    case "0334":
                        return new State_AutoLoadReversalTxLog();
                    case "0531":
                        return new State_AutoLoadQuery();
                        //一般加値類
                    case "0331":
                        return new State_Loading();
                    case "0341":
                        return new State_LoadingTxLog();
                        //一般退貨類
                    case "0631":
                        return new State_PurchaseReturn();
                    case "0641":
                        return new State_PurchaseReturnTxLog();
                    default:
                        return new State_Exit();
                }
            }
            else
            {
                return new State_Exit();
            }
           
        }

        public void Handle(ClientRequestHandler handler)
        {
            handler.ServiceState = this.StateSelect(handler);
        }
    }

    /// <summary>
    /// 依據通訊種別分狀態列表(保留:未使用)
    /// </summary>
    public enum StateType
    {
        /// <summary>
        ///  exit Service
        /// </summary>
        [Description("exit")]
        Exit = 0,
        /// <summary>
        /// ConType:0332 AutoLoad
        /// </summary>
        [Description("自動加值,自動加值TxLog,自動加值查詢")]
        AutoLoad = 1,
        /// <summary>
        /// ConType:0333 AutoLoad TxLog
        /// </summary>
        AutoLoadTxLog = 2,
        /// <summary>
        /// ConType:0334 AutoLoad Reversal TxLog
        /// </summary>
        AutoLoadReversalTxLog = 3,
        /// <summary>
        /// ConType:0531
        /// </summary>
        AutoLoadQuery = 4,
        /// <summary>
        /// ConType:0331 一般加値
        /// </summary>
        Loading = 5,
        /// <summary>
        /// ConType:0341 一般加値 TxLog
        /// </summary>
        LoadingTxLog = 6,
        /// <summary>
        /// ConType:0631 購貨取消
        /// </summary>
        PurchaseReturn = 7,
        /// <summary>
        /// ConType:0641 購貨取消 TxLog
        /// </summary>
        PurchaseReturnTxLog = 8
    }
}
