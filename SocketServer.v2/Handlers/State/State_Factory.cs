using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketServer.v2.Handlers.State
{
    public class State_Factory : IState
    {
        private byte[] receiveData { get; set; }

        public State_Factory(byte[] receiveData)
        {
            this.receiveData = receiveData;
        }
        public IState StateSelect(ClientRequestHandler handler)
        {
            string conType = string.Empty;
            handler.Request = Encoding.ASCII.GetString(this.receiveData);
            Console.WriteLine("State Factory Select Request:{0}", handler.Request);
            if (handler.Request.Length > 10)
            {
                conType = handler.Request.Substring(5, 4);//0332,0333,0334,0531,0331,0341,0631,0641
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
                        break;
                        //一般加値類
                    case "0331":
                        break;
                    case "0341":
                        break;
                        //一般退貨類
                    case "0631":
                        break;
                    case "0641":
                        break;
                    default:
                        return new State_Exit();
                }
                return new State_Exit();
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
    /// 依據通訊種別分狀態列表
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
