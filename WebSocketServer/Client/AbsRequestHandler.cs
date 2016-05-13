using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//socket
using System.Net.Sockets;
//state
using WebSocketServer.Client.State;
//count time spend
using System.Diagnostics;

namespace WebSocketServer.Client
{
    public class AbsRequestHandler : IRequestHandler
    {
        #region Feild
        private bool isUsed;
        private Socket _client;
        /// <summary>
        /// Client Socket 的接收逾時設定
        /// </summary>
        protected int receiveTimeout = 30000; // Set to 30000 ms
        /// <summary>
        /// Client Socket 的送出逾時設定
        /// </summary>
        protected int sendTimeout = 30000; // Set to 30000 ms
        private bool hasTimeout;
        #endregion

        #region Property
        /// <summary>
        /// Client Number
        /// </summary>
        public int ClientNo { get; set; }
        /// <summary>
        /// 接收的資料
        /// </summary>
        public byte[] ReceiveData;
        public Socket ClientSocket
        {
            get
            {
                return this._client;
            }
            set
            {
                this._client = value;
                if (hasTimeout && (null != this._client))
                {
                    this.ClientSocket.ReceiveTimeout = this.receiveTimeout;
                    this.ClientSocket.SendTimeout = this.sendTimeout;
                }
            }
        }
        /// <summary>
        /// Service state
        /// </summary>
        public virtual IState ServiceState { get; set; }
        /// <summary>
        /// Client處理終止的flag
        /// </summary>
        protected virtual bool KeepService { get; set; }
        /// <summary>
        /// timer
        /// </summary>
        protected Stopwatch timer { get; set; }
        #endregion
        
        public virtual void DoCommunicate()
        {
            this.ReceiveData = new byte[1024];
            this.timer.Restart();
            do
            {
                this.timer.Restart();
                this.ServiceState.Handle(this);
                Logger.WriteLog("TimeSpend:" + this.timer.ElapsedMilliseconds.ToString() + "ms");
                this.timer.Stop();
            }
            while (this.KeepService);
            this.timer.Stop();
            Logger.WriteLog("TimeSpend:" + this.timer.ElapsedMilliseconds.ToString() + "ms");
        }

        public virtual void CancelAsync()
        {
            if (null != this.ClientSocket)
            {
                try
                {
                    this.ClientSocket.Shutdown(SocketShutdown.Both);

                }
                catch (SocketException sckEx)
                {
                    Logger.WriteLog("[SocketException]" + sckEx.Message);
                }
                finally
                {
                    this.ClientSocket.Close(1000);
                    //清除本次狀態暫存的數據
                    this.ClientSocket = null;//清除本次的socket物件
                    this.KeepService = false;//用來離開state迴圈
                    this.ReceiveData = null;//清除本次收到的request byte array
                }
            }
        }
    }
}
