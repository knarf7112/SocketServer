using System;
using System.Collections.Generic;
//
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.ComponentModel;
using Common.Logging;
using System.Collections.Concurrent;
using SocketServer.Handlers.State;
using SocketServer.Handlers;
//
//using MqTest.Consumer;


namespace SocketServer
{
    /// <summary>
    /// 
    /// </summary>
    public class AsyncMultiSocketServer : ISocketServer
    {
        #region Field
        // 紀錄Log
        private static readonly ILog log = LogManager.GetLogger(typeof(AsyncMultiSocketServer));
        //主服務Socket
        private Socket mainSocket;
        //主服務終止flag
        private bool KeepSocketServerAlive;
        //存放Client的字典檔
        private IDictionary<int, IClientRequestHandler> dicHandler;
        //Client Count
        private int clientNo;
        //監聽Port
        private int port;
        //服務名稱
        public string stateName;
        //監聽的IP資訊
        private IPAddress listenIP;
        /// <summary>
        /// 管理主Socket服務的背景執行緒物件
        /// </summary>
        private BackgroundWorker bgWorker;
        //for lock
        private Object cLock = new Object();

        private Object dLock = new Object();

        #endregion

        //public ClientRequestHandler ClientRequestHandler { get; set; }//*
        #region Construcotr
        /// <summary>
        /// Socket Server Constructor
        /// </summary>
        /// <param name="port">socket listen port</param>
        /// <param name="start">是否直接啟動</param>
        /// <param name="stateName">要啟動的服務名稱,看ServiceFactory.cs的enum ServiceSelect列表</param>
        /// <param name="listenIP">socket listen ip [null=AnyIP]</param>
        public AsyncMultiSocketServer(int port, bool start,string stateName, string listenIP = null)
        {
            this.port = port;
            this.stateName = stateName;
            //存放Thread的字典檔
            this.dicHandler = new ConcurrentDictionary<int, IClientRequestHandler>();
            log.Debug(">> Port Number: " + this.port);
            log.Debug(">> State name: " + this.stateName);
            log.Debug(">> Start : " + (start ? "true" : "false"));
            if (start)
            {
                this.Start();
            }

            //是否只監聽本機
            if (listenIP != null)
            {
                IPAddress ip = null;
                if(IPAddress.TryParse(listenIP,out ip))
                {
                    this.listenIP = ip;
                }
                else
                {
                    log.Error("IP Convert Failed => Listen AnyIP");
                    this.listenIP = IPAddress.Any;
                }
            }
            else
            {
                this.listenIP = IPAddress.Any;
            }
            // Init BackGroundWorker and add Event
            this.bgWorker = new BackgroundWorker()
            {
                WorkerSupportsCancellation = true//指定此BackgroundWorker可取消工作中的Thread
            };
            this.bgWorker.DoWork += bgWorker_DoWork;//工作觸發指定方法
            this.bgWorker.RunWorkerCompleted += bgWorker_RunWorkerCompleted;//工作完成觸發指定方法
        }
        /// <summary>
        /// Specified Prot and ServiceName Then run Constructor AsyncMultiSocketServer
        /// Default Encode: UTF8
        /// </summary>
        /// <param name="port"></param>
        /// <param name="serviceName"></param>
        public AsyncMultiSocketServer(int port, string serviceName)
            : this(port, false, serviceName)
        {

        }
        /// <summary>
        /// Run Constructor AsyncMultiSocketServer By Prot
        /// Default ServiceName: "Authenticate",Default Encode:System Default
        /// </summary>
        /// <param name="port">Listen Port</param>
        public AsyncMultiSocketServer(int port)
            : this(port, false, "Authenticate")
        {

        }
        #endregion

        #region public Method
        /// <summary>
        /// 啟動服務
        /// </summary>
        public void Start()
        {
            this.KeepSocketServerAlive = true;
            this.clientNo = 0;
            //若 Socket 正在工作，則停止
            if (this.mainSocket != null)
            {
                this.mainSocket.Close();
            }
            
            //this.mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //this.mainSocket.Bind(new IPEndPoint(this.listenIP, this.port));
            //this.mainSocket.Listen(50);
            log.Debug(">> Server Start ...");
            //如果Backgroundworker不忙則開始執行背景作業(Thread work)
            if (!this.bgWorker.IsBusy)
            {
                this.bgWorker.RunWorkerAsync();
            }
            else
            {
                System.Threading.Thread.Sleep(5);
                this.Start();
            }
        }
        /// <summary>
        /// 停止服務
        /// </summary>
        public void Stop()
        {
            if (this.KeepSocketServerAlive)
            {
                log.Debug(">> Set Flag ==> False ");
                this.KeepSocketServerAlive = false;
            }
            if (this.bgWorker.IsBusy)
            {
                this.bgWorker.CancelAsync();
            }

            if (this.mainSocket != null)
            {
                try
                {
                    this.mainSocket.Close();
                    log.Debug(">> Server Stop ...");
                }
                catch (SocketException ex)
                {
                    log.Error(">> Main Socket Close Failed : " + ex.Message + ":" + ex.ErrorCode);
                    this.mainSocket.Shutdown(SocketShutdown.Both);
                    this.mainSocket.Dispose();
                    log.Error(">> Main Socket Dispose ...");
                }
                finally
                {
                    this.mainSocket = null;
                    //log.Debug(">> Main Socket Set Null");
                }
            }
        }

        /// <summary>
        ///   Remove a handler by it's client no.
        /// </summary>
        /// <param name="clientNo"></param>
        public void RemoveClient(int clientNo)
        {
            lock (dLock)
            {
                if (this.dicHandler.ContainsKey(clientNo))
                {
                    //子物件清除父層物件指標
                    ((ClientRequestHandler)this.dicHandler[clientNo]).SocketServer = null;
                    //取消子物件的Thread運行
                    (this.dicHandler[clientNo]).CancelAsync();
                    //從字典檔移除子物件
                    this.dicHandler.Remove(clientNo);
                    //log.Debug(">> Remove: " + clientNo);
                }
            }
        }
        #endregion

        #region Event
        /// <summary>
        /// 服務完成後要做的事
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                if (e.Cancelled)
                {
                    log.Debug(">> Main Socket Task Cancelled!");
                }
                //else
                //{
                //    log.Info(">> Main Socket Task completed!");
                //}
            }
            else
            {
                log.Error(">> Main Socket Task failed:" + e.Error.StackTrace);
            }
        }
        /// <summary>
        /// 產生一個背景執行緒來執行main Socket(某個Service)的連線服務
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                //若 Socket 正在工作，則停止
                if (this.mainSocket != null)
                {
                    this.mainSocket.Close();
                }
                // 1.建立Socket物件,接受雙向的Tcp/Ip協議
                this.mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // 2.建立允許連入的IP資訊關聯
                this.mainSocket.Bind(new IPEndPoint(this.listenIP, this.port));
                // 3.開始進入監聽狀態並監聽嘗試連入的連線 //之前測試,當等待的連線有作資料傳送時,可能會在暫存等待區裡面建立一個新的Socket並將收到的資料存放於此Socket的buffer中,當輪到此連線連入時就可以讀取buffer中的Client傳入資料
                this.mainSocket.Listen(3000);//等待連接的最高上限會因OS不同(或ISP限制)而有不同限制
                log.Debug(">> Server Start ...");

                //持續監聽此服務 
                while (this.KeepSocketServerAlive)
                {
                    // 4.開始等待連入(Blocking)
                    Socket socket4Client = this.mainSocket.Accept();
                    lock (cLock)
                    {
                        if (this.clientNo == Int32.MaxValue)
                        {
                            this.clientNo = 0;
                        }
                        else
                        {
                            this.clientNo += 1;
                        }
                        //
                        //log.Info
                        //(
                        //      " >> "
                        //    + "Client["
                        //    + ((IPEndPoint)socket4Client.RemoteEndPoint).ToString()
                        //    + "] Request No:"
                        //    + Convert.ToString(this.clientNo)
                        //    + " started!"
                        //);
                        // 5.產生一個Client物件並利用State來處理該作的事(並代入父層物件)
                        ClientRequestHandler client = new ClientRequestHandler(this.clientNo, socket4Client, this, ServiceFactory.GetService(this.stateName));
                        //this.dicHandler.Add(this.clientNo, this.clientRequestHandler);//*
                        // 6.加入Client管理列表的字典檔
                        this.dicHandler.Add(this.clientNo, client);
                        // 7.產生一個背景執行緒執行此Client要處理的State
                        this.dicHandler[this.clientNo].DoCommunicate();
                        
                    }
                }
                log.Debug(">> Server Stop...");
            }
            catch (Exception ex)
            {
                log.Error(">> Main Socket Error : " + ex.StackTrace);
                this.Stop();
            }
            this.RemoveAllClients();
        }
        #endregion
        
        /// <summary>
        /// 移除所有被管理的Client物件
        /// </summary>
        private void RemoveAllClients()
        {
            int[] copyKeys = new int[this.dicHandler.Count];
            this.dicHandler.Keys.CopyTo(copyKeys, 0);
            //若用原來的dic.Keys來iterate因為每次都remove掉了所以每次的keys數都會改變,則會有exception
            foreach (int clientNo in copyKeys)
            {
                this.RemoveClient(clientNo);
            }
        }
    }
}
