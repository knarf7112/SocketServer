﻿using SocketServer.v2.Handlers.State;
using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace SocketServer.v2.Handlers
{
    public abstract class AbsClientRequestHandler : IClientRequestHandler
    {

        #region Static object
        //private static readonly ILog log = LogManager.GetLogger(typeof(AbsClientRequestHandler));
        private bool isUsed;
        #endregion

        #region Property
        /// <summary>
        ///   socket client 識別號碼   
        /// </summary>
        public int ClientNo { get; set; }

        /// <summary>
        /// 使用次數
        /// </summary>
        public int UseCount { get; set; }

        /// <summary>
        ///   socket client reuqest
        /// </summary>
        public Socket ClientSocket { get; set; }
        /// <summary>
        /// main socket
        /// </summary>
        public Socket MainSocket { get; set; }
        /// <summary>
        /// check client handler have used
        /// </summary>
        public bool IsUsed { 
            get 
            { 
                return this.isUsed; 
            }
            set
            {
                this.isUsed = value;
                //若client handler被抓去用就要把keep state的 flag 打開
                if(this.isUsed)
                {
                    this.KeepService = true;
                }
            }
        }
        /// <summary>
        /// Request String [ASCII]
        /// </summary>
        public string Request { get; set; }
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
        /// <summary>
        /// Client Socket 的逾時設定
        /// </summary>
        protected int receiveTimeout = 5000; // Set to 30000 ms
        protected int sendTimeout = 5000; // Set to 30000 ms
        #endregion

        #region Constructor
        public AbsClientRequestHandler(int clientNo)
        {
            this.ClientNo = clientNo;
            this.IsUsed = false;
            //this.ClientSocket.ReceiveTimeout = this.receiveTimeout;
            //this.ClientSocket.SendTimeout = this.sendTimeout;
            this.KeepService = true;
            this.timer = new Stopwatch();
        }
        #endregion

        #region Public Method 繼承IClientRequestHandler介面的實作方法
        /// <summary>
        /// 開始執行通訊作業
        /// </summary>
        public virtual void DoCommunicate()
        {
            //觸發backgroundWorker的DoWork事件去委派執行backgroundWorker_DoWork方法
             ;
        }
        /// <summary>
        /// 取消此Client的連線作業
        /// </summary>
        public void CancelAsync()
        {
            if (this.ClientSocket != null)
            {
                try
                {
                    Console.WriteLine("開始關閉Client連線:{0}", this.ClientNo);
                    this.ClientSocket.Shutdown(SocketShutdown.Both);
                    this.ClientSocket.Close();
                }
                catch (SocketException sckEx)
                {
                    Console.WriteLine("[CancelAsync] Socket Error:" + sckEx.Message + Environment.NewLine + sckEx.StackTrace);
                    //log.Error(m=>m(">> [AbsClientRequestHandler][CancelAsync]Client " + this.ClientNo + " Socket Close Failed: " + ex.Message));
                }
                finally
                {
                    if (this.UseCount >= Int32.MaxValue) 
                    { 
                        this.UseCount = 0; 
                    }
                    this.UseCount += 1;
                    this.ClientSocket = null;
                    this.IsUsed = false;
                    this.KeepService = false;
                    this.Request = string.Empty;
                }
            }
            
        }
        #endregion
    }
}
