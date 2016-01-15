using SocketServer.v2.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SocketServer.v2
{
    public class AsyncMultiSocketServer : ISocketServer
    {
        #region Field
        // 紀錄Log
        //private static readonly ILog log = LogManager.GetLogger(typeof(AsyncMultiSocketServer));
        //主服務Socket
        private Socket mainSocket;
        //主服務終止flag
        private bool KeepSocketServerAlive;
        //client handler集合的管理者
        private ClientRequestManager clientManager;
        //Client Count
        private int clientNo;
        //監聽Port
        private int port;
        //服務名稱
        public string stateName;
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
        /// <param name="stateName">要啟動的服務名稱,看ServiceFactory.cs的enum ServiceSelect列表</param>
        /// <param name="listenIP">socket listen ip [null=AnyIP]</param>
        public AsyncMultiSocketServer(int port, bool start,string stateName, string listenIP = null)
        {
            this.port = port;
            this.stateName = stateName;
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
        /// Default ServiceName: "Authenticate",Default Encode:System Default
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
                this.AcceptConnectEvent.Set();
                this.AcceptConnectEvent.Dispose();
                //log.Debug(">> Set Flag ==> False ");
                Console.WriteLine(">> Set Flag ==> False ");
                this.KeepSocketServerAlive = false;
            }

            Console.WriteLine(">> {0} Service Stop ...", this.stateName);
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
                    Console.WriteLine(">> Main Socket Close Failed : " + ex.Message + ":" + ex.ErrorCode);
                    //log.Error(">> Main Socket Close Failed : " + ex.Message + ":" + ex.ErrorCode);
                    this.mainSocket.Shutdown(SocketShutdown.Both);
                    this.mainSocket.Dispose();
                    //log.Error(">> Main Socket Dispose ...");
                }
                finally
                {
                    this.mainSocket = null;
                    this.clientManager = null;
                    //log.Debug(">> Main Socket Set Null");
                }
            }
            this.clientManager.ClearAll();
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
            this.clientNo = 0;
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
                //log.Debug(">> Server Start ...");
                //Console.WriteLine("StartService Thread Id:{0}  background:{1}", Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.IsBackground.ToString());
                Console.WriteLine(">> Server Start ...{0}", this.stateName);
                //Async
                do
                {
                    this.AcceptConnectEvent.Reset();
                    lock (cLock)
                    {
                        this.clientNo++;
                    }
                    ClientRequestHandler client = new ClientRequestHandler(this.clientNo);// (ClientRequestHandler)ClientRequestHandleManager.GetInstance(this.mainSocket);
                    if (client.MainSocket == null)
                    {
                        client.MainSocket = this.mainSocket;
                    }
                    this.mainSocket.BeginAccept(AcceptCallback, client);//callback 要帶的狀態或數據要一起帶到callback,去就回不來了
                    this.AcceptConnectEvent.WaitOne();
                    //log.Debug(m => m("Client No"));
                    Console.WriteLine("Next Loop ... ");

                }
                while (this.KeepSocketServerAlive);
                Console.WriteLine("End Start...");
                //log.Debug("End Start...");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Start Failed...{0}", ex.Message);
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
                if (ar.IsCompleted)
                {
                    client = ((ClientRequestHandler)ar.AsyncState);
                    client.ClientSocket = client.MainSocket.EndAccept(ar);
                    setFlag = this.AcceptConnectEvent.Set();
                    //開始執行工作流程
                    client.DoCommunicate();
                }
            }
            catch (SocketException sckEx)
            {
                Console.WriteLine("[AcceptCallback]Socket Error:" + sckEx.Message + Environment.NewLine + sckEx.StackTrace);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AcceptCallback]Callback Error:" + ex.Message + Environment.NewLine + ex.StackTrace);
                
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
                        Console.WriteLine(" AcceptConnectEvent Error: {0}", objDisposeEx.Message);
                    }
                }
            }
        }
        #endregion
    }
}
