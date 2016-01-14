using System;
using System.ComponentModel;
using System.Net.Sockets;
//
using Common.Logging;
//
using SocketServer.Handlers.State;
//using MqTest.Consumer;
using System.Threading;
using System.Diagnostics;


namespace SocketServer.Handlers
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class AbsClientRequestHandler : IClientRequestHandler
    {

        #region Static object
        private static readonly ILog log = LogManager.GetLogger(typeof(AbsClientRequestHandler));

        private static object cLock = new object();

        private static int totalClient = 0;

        private static object totalLock = new object();
        #endregion

        #region Property
        /// <summary>
        ///   socket client 識別號碼   
        /// </summary>
        public int ClientNo { get; set; }

        /// <summary>
        ///   socket client reuqest
        /// </summary>
        public virtual Socket ClientSocket { get; set; }

        /// <summary>
        /// main socket server(parent object)
        /// </summary>
        public virtual ISocketServer SocketServer { get; set; }

        public virtual IState ServiceState { get; set; }
        /// <summary>
        /// Client處理終止的flag
        /// </summary>
        public virtual bool KeepService { get; set; }

        /// <summary>
        ///   background worker 
        /// </summary>
        protected BackgroundWorker backgroundWorker { get; set; }
        /// <summary>
        /// Client Socket 的逾時設定
        /// </summary>
        protected int readTimeout = 5000; // Set to 30000 ms
        protected int writeTimeout = 5000; // Set to 30000 ms
        #endregion

        #region Public Method 繼承IClientRequestHandler介面的實作方法
        /// <summary>
        /// 開始產生一個Thread執行Client要做的事
        /// </summary>
        public void DoCommunicate()
        {
            //觸發backgroundWorker的DoWork事件去委派執行backgroundWorker_DoWork方法
            if (!this.backgroundWorker.IsBusy)
            {
                this.backgroundWorker.RunWorkerAsync();
            }
            else
            {
                Thread.Yield();
                this.DoCommunicate();
            }
        }
        /// <summary>
        /// 取消此Client的連線作業
        /// </summary>
        public void CancelAsync()
        {
            //backgroundworker is running
            if (this.backgroundWorker.IsBusy)
            {
                this.backgroundWorker.CancelAsync();
                if (this.ClientSocket != null)
                {
                    try
                    {
                        this.ClientSocket.Shutdown(SocketShutdown.Both);
                        this.ClientSocket.Close();
                        this.ClientSocket = null;
                    }
                    catch (SocketException ex)
                    {
                        log.Error(">> [AbsClientRequestHandler][CancelAsync]Client " + this.ClientNo + " Socket Close Failed: " + ex.Message);
                    }
                }
            }
        }
        #endregion

        #region Event 用BackgroundWorker來分送任務
        /// <summary>
        /// 完成後
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public virtual void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if (e.Error == null)
                {
                    if (e.Cancelled)
                    {
                        //Console.WriteLine(">> [" + e.Result.ToString() + "] Client " + this.ClientNo + " Task Cancelled!");
                        log.Error(">> [" + e.Result.ToString() + "] Client " + this.ClientNo + " Task Cancelled!");
                    }
                    else
                    {
                        //Console.WriteLine(">> [" + e.Result.ToString() + "] Client " + this.ClientNo + " Task completed!");
                        //log.Info(">> [" + e.Result.ToString() + "] Client " + this.ClientNo + " Task completed!");
                    }
                }
                else
                {
                    log.Error(">> [backgroundWorker_RunWorkerCompleted][" + e.Result.ToString() + "] Client " + this.ClientNo + " Task failed:" + e.Error.ToString());
                }
                //lock (totalLock)
                //{
                //    totalClient--;
                //}
                ////紀錄當前仍在運算的Thead
                //log.Debug(">> [backgroundWorker_RunWorkerCompleted]Total Client:" + totalClient);
            }
            catch (Exception ex)
            {
                log.Error("[AbsClientRequestHandler][backgroundWorker_RunWorkerCompleted] Error:" + ex.ToString());
            }
        }
        /// <summary>
        /// (背景作業)開始執行Client的工作處理(使用Design Pattern的State一直連續做下去)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public virtual void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //lock (totalLock)
            //{
            //    totalClient++;
            //}
            e.Result = this.ServiceState.GetType().Name;
            //DateTime startTime = DateTime.Now;
            Stopwatch timer = new Stopwatch();
            timer.Start();
            while (this.KeepService)
            {
                this.ServiceState.Handle(this);
            }
            //DateTime endTime = DateTime.Now;
            timer.Stop();

            log.Debug(">> [" + e.Result.ToString() + "] Task Spend:" + (timer.ElapsedTicks /(decimal) Stopwatch.Frequency).ToString("f3") + "s");
        }
        #endregion
    }
}

