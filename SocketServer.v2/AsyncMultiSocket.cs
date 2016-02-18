using SocketServer.v2.Handlers;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
//log
using Common.Logging;

namespace SocketServer.v2
{
    /// <summary>
    /// 非同步Socket Server v2
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
        //client handler集合的管理者
        private ClientRequestManager clientManager;
        //監聽Port
        private int port;
        //服務名稱
        public string serviceName;
        //監聽的IP資訊
        private IPAddress listenIP;
        ///blocking server then accept connection
        private ManualResetEvent AcceptConnectEvent;
        //for lock
        private Object cLock = new Object();

        #endregion

        //public ClientRequestHandler ClientRequestHandler { get; set; }//*
        #region Construcotr
        /// <summary>
        /// Socket Server Constructor
        /// </summary>
        /// <param name="port">socket listen port</param>
        /// <param name="start">是否直接啟動</param>
        /// <param name="serviceName">要啟動的服務名稱,看ServiceFactory.cs的enum ServiceSelect列表</param>
        /// <param name="listenIP">socket listen ip [null=AnyIP]</param>
        public AsyncMultiSocketServer(int port, bool start, string serviceName, string listenIP = null)
        {
            this.port = port;
            this.serviceName = serviceName;
            this.AcceptConnectEvent = new ManualResetEvent(false);
            this.clientManager = new ClientRequestManager();
            //log.Debug(">> Port Number: " + this.port);
            //log.Debug(">> State name: " + this.stateName);
            //log.Debug(">> Start : " + (start ? "true" : "false"));
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
                    //log.Error("IP Convert Failed => Listen AnyIP");
                    this.listenIP = IPAddress.Any;
                }
            }
            else
            {
                this.listenIP = IPAddress.Any;
            }

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
        /// Default ServiceName: "AutoLoad",Default Encode:System Default
        /// </summary>
        /// <param name="port">Listen Port</param>
        public AsyncMultiSocketServer(int port)
            : this(port, false, "AutoLoad")
        {

        }
        #endregion

        #region Public Method
        /// <summary>
        /// 啟動服務
        /// </summary>
        public void Start()
        {
            //Console.WriteLine("Start Thread Id:{0} background:{1}", Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.IsBackground.ToString());
            //執行任務
            Task.Run(() =>
            {
                this.StartService();//執行Socket Server服務
            });
        }
        
        /// <summary>
        /// 停止服務
        /// </summary>
        public void Stop()
        {
            if (this.KeepSocketServerAlive)
            {
                this.KeepSocketServerAlive = false;
                this.AcceptConnectEvent.Set();
                this.AcceptConnectEvent.Dispose();
                log.Info(m => m(">> Set Flag ==> False and dispose Main ManualResetEvent"));
                //Console.WriteLine(">> Set Flag ==> False ");
            }

            //Console.WriteLine(">> Service Stop ...{0}", this.serviceName);
            log.Info(m => m(">> Service Stop ...{0}", this.serviceName));
            if (this.mainSocket != null)
            {
                try
                {
                    this.mainSocket.Close();
                    //log.Debug(">> Server Stop ...");
                    //Console.WriteLine(">> {0} Server Stop ...",this.stateName);
                }
                catch (SocketException ex)
                {
                    //Console.WriteLine(">> Main Socket Close Failed : " + ex.Message + ":" + ex.ErrorCode);
                    //log.Error(">> Main Socket Close Failed : " + ex.Message + ":" + ex.ErrorCode);
                    if (this.mainSocket.Connected)
                    {
                        this.mainSocket.Shutdown(SocketShutdown.Both);
                    }
                    this.mainSocket.Dispose();
                    log.Error(m => m(">> Main Socket Dispose ...Error:{0}", ex.Message));
                }
                finally
                {
                    this.mainSocket = null;
                    //log.Debug(">> Main Socket Set Null");
                }
            }
            //清除Client Manager的所有數據
            this.clientManager.ClearAll();
            this.clientManager = null;
        }

        public void RemoveClient(int clientNo)
        {
            this.clientManager.RemoveClient(clientNo);
        }
        
        #endregion

        #region Private Method
        /// <summary>
        /// 啟動Async Socket Server服務
        /// </summary>
        private void StartService()
        {
            this.KeepSocketServerAlive = true;
            //若 Socket 正在工作，則停止
            if (this.mainSocket != null)
            {
                this.mainSocket.Close();
            }
            try
            {
                this.mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.mainSocket.Bind(new IPEndPoint(this.listenIP, this.port));
                this.mainSocket.Listen(5000);
                //Console.WriteLine("StartService Thread Id:{0}  background:{1}", Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.IsBackground.ToString());
                //Console.WriteLine(">> Server Start ...{0}", this.stateName);
                log.Info(m => m(">> Server Start ...{0}", this.serviceName));
                //Async
                do
                {
                    this.AcceptConnectEvent.Reset();

                    ClientRequestHandler client = (ClientRequestHandler)this.clientManager.GetInstance();//new ClientRequestHandler(this.clientNo);// (ClientRequestHandler)ClientRequestHandleManager.GetInstance(this.mainSocket);
                    if (client.MainSocket == null)
                    {
                        client.MainSocket = this.mainSocket;
                    }
                    IAsyncResult ar = this.mainSocket.BeginAccept(AcceptCallback, client);//callback 要帶的狀態或數據要一起帶到callback,去就回不來了
                    this.AcceptConnectEvent.WaitOne();//TODO...看之後要不要加入釋放Thread wait用的時間間隔
                    log.Debug(m => m("[StartService]Next Waiting for ... "));
                    //Console.WriteLine("Next Loop ... ");
                    
                }
                while (this.KeepSocketServerAlive);
                //Console.WriteLine("End Start...");
                log.Debug("End Start...");
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Start Failed...{0}", ex.Message);
                log.Error(m => m("Start Failed...{0}", ex.Message));
            }
        }
        /// <summary>
        /// Asynchronization Callback
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptCallback(IAsyncResult ar)
        {
            ClientRequestHandler client = null;
            bool setFlag = false;
            try
            {
                //連線等待1秒
                if (ar.AsyncWaitHandle.WaitOne(1000))
                {
                    if (ar.IsCompleted)
                    {
                        client = ((ClientRequestHandler)ar.AsyncState);
                        client.ClientSocket = client.MainSocket.EndAccept(ar);
                        setFlag = this.AcceptConnectEvent.Set();
                        //開始執行工作流程
                        client.DoCommunicate();
                    }
                }
                else
                {
                    //目前測試不出這種狀況,或許需要拉到mainSocket.BeginAccept下跑才有用
                    //Console.WriteLine("[掛了沒連上]");
                    log.Error(m=>m("[AcceptCallback]建立連線逾時"));
                    client = ((ClientRequestHandler)ar.AsyncState);
                    client.ClientSocket = client.MainSocket.EndAccept(ar);
                    client.CancelAsync();
                }
            }
            catch (SocketException sckEx)
            {
                //Console.WriteLine("[AcceptCallback]Socket Error:" + sckEx.Message + Environment.NewLine + sckEx.StackTrace);
                log.Error(m => m("[AcceptCallback]Socket Error:" + sckEx.Message + Environment.NewLine + sckEx.StackTrace));
            }
            catch (Exception ex)
            {
                //Console.WriteLine("[AcceptCallback]Callback Error:" + ex.Message + Environment.NewLine + ex.StackTrace);
                log.Error(m => m("[AcceptCallback]Callback Error:" + ex.Message + Environment.NewLine + ex.StackTrace));
            }
            finally
            {
                //this.AcceptConnectEvent.Set();
                if (!setFlag)
                {
                    try
                    {
                        this.AcceptConnectEvent.Set();
                    }
                    catch (ObjectDisposedException objDisposeEx)
                    {
                        //Console.WriteLine(" AcceptConnectEvent Error: {0}", objDisposeEx.Message);
                        log.Error(m => m(" AcceptConnectEvent Error: {0}", objDisposeEx.Message));
                    }
                }
            }
        }
        #endregion
    }
}
