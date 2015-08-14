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
    public class AsyncMultiSocketServer : ISocketServer
    {
        #region Field
        // 工作進行中產生之訊息
        private static readonly ILog log = LogManager.GetLogger(typeof(AsyncMultiSocketServer));
        
        private Socket mainSocket;

        private bool KeepSocketServerAlive;

        private IDictionary<int, IClientRequestHandler> dicHandler;

        private int clientNo;

        private int port;

        public string stateName;

        private Encoding encoding;

        private IPAddress listenIP;

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
        /// <param name="encoding">設定編碼</param>
        /// <param name="listenIP">socket listen ip [null=Any]</param>
        public AsyncMultiSocketServer(int port, bool start,string stateName, Encoding encoding, string listenIP = null)
        {
            this.port = port;
            this.encoding = encoding;
            this.stateName = stateName;
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
                WorkerSupportsCancellation = true
            };
            this.bgWorker.DoWork += bgWorker_DoWork;
            this.bgWorker.RunWorkerCompleted += bgWorker_RunWorkerCompleted;
        }
        public AsyncMultiSocketServer(int port, string serviceName)
            : this(port, false, serviceName, Encoding.UTF8)
        {

        }
        public AsyncMultiSocketServer(int port)
            : this(port, false, "Authenticate", Encoding.Default)
        {

        }
        #endregion

        #region public Method
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
                    //this.mainSocket = null;
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
                    (this.dicHandler[clientNo]).CancelAsync();
                    this.dicHandler.Remove(clientNo);
                    //log.Debug(">> Remove: " + clientNo);
                }
            }
        }
        #endregion

        #region Event
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
        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                //若 Socket 正在工作，則停止
                if (this.mainSocket != null)
                {
                    this.mainSocket.Close();
                }
                //
                this.mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.mainSocket.Bind(new IPEndPoint(this.listenIP, this.port));
                this.mainSocket.Listen(3000);
                log.Debug(">> Server Start ...");

                //持續監聽 
                while (this.KeepSocketServerAlive)
                {
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
                        ClientRequestHandler client = new ClientRequestHandler(this.clientNo, socket4Client, this, ServiceFactory.GetService(this.stateName));
                        //this.dicHandler.Add(this.clientNo, this.clientRequestHandler);//*
                        this.dicHandler.Add(this.clientNo, client);
                        this.dicHandler[this.clientNo].DoCommunicate();
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

        public Encoding GetEncoding()
        {
            return this.encoding;
        }
        
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
