using System;
using System.ComponentModel;
using System.Net.Sockets;
//
using Common.Logging;
//
using SocketServer.Handlers.State;
//using MqTest.Consumer;
using System.Threading;


namespace SocketServer.Handlers
{
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
        ///   socket server
        /// </summary>
        public virtual ISocketServer SocketServer { get; set; }

        public virtual IState ServiceState { get; set; }

        public virtual bool KeepService { get; set; }

        /// <summary>
        ///   background worker 
        /// </summary>
        protected BackgroundWorker backgroundWorker { get; set; }

        protected int readTimeout = 5000; // Set to 30000 ms
        protected int writeTimeout = 5000; // Set to 30000 ms
        #endregion

        #region Public Method 繼承IClientRequestHandler介面的實作方法
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
                //log.Debug(">> [backgroundWorker_RunWorkerCompleted]Total Client:" + totalClient);
            }
            catch (Exception ex)
            {
                log.Error("[AbsClientRequestHandler][backgroundWorker_RunWorkerCompleted] Error:" + ex.ToString());
            }
        }

        public virtual void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //lock (totalLock)
            //{
            //    totalClient++;
            //}
            //e.Result = this.ServiceState.GetType().Name;
            //DateTime startTime = DateTime.Now;
            while (this.KeepService)
            {
                this.ServiceState.Handle(this);
            }
            //DateTime endTime = DateTime.Now;
            //log.Debug(">> [" + e.Result.ToString() + "] Task Spend:" + (endTime - startTime).TotalMilliseconds.ToString() + "ms");
        }
    }
        #endregion
}

