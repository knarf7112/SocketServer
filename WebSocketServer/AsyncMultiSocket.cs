using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//socket object
using System.Net.Sockets;
//IPAddress
using System.Net;
//
using System.Threading;
//get current method
using System.Runtime.CompilerServices;
//StackTrace Trace method frame
using System.Diagnostics;

namespace WebSocketServer
{
    //public delegate void WriteLog(string msg);
    public class AsyncMultiSocket : ISocketServer
    {
        #region Field
        private Socket _mainSocket;
        private bool SocketInitial;
        private bool KeepServerAlive;
        private static readonly int BACKLOG = 2000;
        #endregion

        #region Property
        /// <summary>
        /// Socket Listen Port
        /// </summary>
        public int Port { get; private set; }
        /// <summary>
        /// 服務名稱
        /// </summary>
        public string ServiceName { get; private set; }
        /// <summary>
        /// listen address
        /// </summary>
        public IPAddress Listen_IPAddress { get; private set; }
        #endregion

        #region Constructor
        public AsyncMultiSocket(int port, string serviceName, bool start, string listenIP = null)
        {
            IPAddress ip = null;
            this.SocketInitial = false;
            this.KeepServerAlive = true;
            this.Port = port;
            if (start)
            {
                this.Start();
            }
            if (null == listenIP || !IPAddress.TryParse(listenIP, out ip))
            {
                this.Listen_IPAddress = IPAddress.Any;
            }
            else
            {
                this.Listen_IPAddress = ip;
            }
        }
        public AsyncMultiSocket(int port, string serviceName) : this(port, serviceName, false) { }
        #endregion
        #region Public Method
        public void Start()
        {
            Task.Factory.StartNew(this.StartService);
        }

        public void Stop()
        {
            try
            {
                this.KeepServerAlive = false;
                this.SocketInitial = false;
                if (null != this._mainSocket)
                {
                    //if()
                    //this._mainSocket.Shutdown(SocketShutdown.Both);//因位主socket不負責傳送與接收,所以根本沒有任何連接
                    this._mainSocket.Close(1000);
                    //this._mainSocket.Dispose();
                }
            }
            catch (SocketException sckEx)
            {
                this.WriteLog("[Error]" + sckEx.Message);
            }
            finally
            {
                this._mainSocket = null;
            }
        }

        public void RemoveClient(int clientNo)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region
        protected virtual void StartService()
        {
            try
            {
                if(!this.SocketInitial)
                {
                    this.Initial_Socket();
                }
                WriteLog("Start Service: " + this.ServiceName);
                Logger.WriteLog("Start Service: " + this.ServiceName);
                this.AsyncAccept();
            }
            catch (Exception ex)
            {
                WriteLog("[Error]" + ex.Message);
            }
        }
        protected void AsyncAccept()
        {
            try
            {
                this._mainSocket.BeginAccept(this.AcceptCallback, null);
                WriteLog("one client socket waiting for ...");
                Logger.WriteLog("one client socket waiting for ...");
            }
            catch (SocketException sckEx)
            {
                WriteLog("[Error]" + sckEx.Message);
            }
        }

        protected virtual void AcceptCallback(IAsyncResult ar)
        {
            if (this.KeepServerAlive)
                this.AsyncAccept();

            try
            {
                if (ar.IsCompleted && null != this._mainSocket)
                {
                    
                    //Socket mainSoket = (Socket)ar.AsyncState;
                    Socket client = this._mainSocket.EndAccept(ar);//mainSoket.EndAccept(ar);
                    //string msg = "Hello";
                    //byte[] send_data = Encoding.UTF8.GetBytes(msg);
                    //int sendLength = client.Send(send_data);
                    byte[] result = new byte[0];
                    int Totallength = 0;
                    while (client.Available > 0)
                    {
                        byte[] buffer = new byte[0x10];
                        int recieveLength = client.Receive(buffer);
                        Totallength += recieveLength;
                        if (recieveLength < buffer.Length)
                            Array.Resize(ref buffer, recieveLength);
                        result = result.Concat(buffer).ToArray();
                        
                    }
                    string recieveData = Encoding.UTF8.GetString(result, 0, Totallength);
                    WriteLog(recieveData);
                    Logger.WriteLog(recieveData);
                    //WriteLog("一個client socket connected");
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Error]" + ex.Message);
                Logger.WriteLog("[Error]" + ex.Message);
            }
        }
        protected void Initial_Socket()
        {
            WriteLog("Initial Main Socket");
            this._mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._mainSocket.Bind(new IPEndPoint(this.Listen_IPAddress, this.Port));
            this._mainSocket.Listen(BACKLOG);
            this.SocketInitial = true;
        }
        #endregion

        #region Event : Write Log
        /// <summary>
        /// 委派給外部去設定寫Log的方法
        /// </summary>
        public virtual WriteLog OnWriteLog { private get; set; }
        /// <summary>
        /// 若有設定委派方法,則輸出內部寫的Log
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void WriteLog(string msg)
        {
            if (null != OnWriteLog) 
            {
                string info = String.Format("{0} [ThreadId:{1}][Method:{2}] ", 
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Thread.CurrentThread.ManagedThreadId, 
                    this.GetCurrentMethod());
                OnWriteLog.Invoke(info + msg);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string GetCurrentMethod()
        {
            //ref:http://stackoverflow.com/questions/2652460/c-sharp-how-to-get-the-name-of-the-current-method-from-code
            StackTrace trace = new StackTrace();
            //取呼叫的方法frame(扣掉GetCurrentMethod和WriteLog方法,所以是第2個)
            StackFrame sf = trace.GetFrame(2);

            return sf.GetMethod().Name;
        }
        #endregion
    }
}
