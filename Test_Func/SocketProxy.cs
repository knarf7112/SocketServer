using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//
using System.Net;
//thread safe dictionary
using System.Collections.Concurrent;
using System.Xml.Linq;

namespace Test_Func
{
    /// <summary>
    /// Socket Proxy Class
    /// </summary>
    public class SocketProxy : IDisposable
    {
        /// <summary>
        /// Main Proxy Server
        /// </summary>
        private Socket mainSck;
        /// <summary>
        /// Proxy Server Asynchronization switch
        /// </summary>
        private ManualResetEvent acceptDone;
        //keep service flag
        private bool KeepServerAlive;
        /// <summary>
        /// 目的端點資訊
        /// </summary>
        private IPEndPoint RemoteIP { get; set; }
        /// <summary>
        /// Proxy Server 端點資訊
        /// </summary>
        private IPEndPoint ServerIP { get; set; }

        private IDictionary<IList<IPEndPoint>, IList<IPEndPoint>> proxyList;

        public SocketProxy()
        {
            this.InitProperties();
        }

        public SocketProxy(int listenPort, string remoteIP, int remotePort)
        {
            //解析遠端,無法解析會拋異常
            IPAddress[] IPs = Dns.GetHostEntry(IPAddress.Parse(remoteIP)).AddressList;
            foreach (IPAddress ip in IPs)
            {
                Console.WriteLine("Dns Resolve's IPAddress: {0}", ip.ToString());
            }
            this.RemoteIP = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);
            this.ServerIP = new IPEndPoint(IPAddress.Any, listenPort);
            this.mainSck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.acceptDone = new ManualResetEvent(false);
            this.proxyList = new ConcurrentDictionary<IList<IPEndPoint>, IList<IPEndPoint>>();
            this.KeepServerAlive = true;
        }
        private void InitProperties()
        {
            this.ServerIP = new IPEndPoint(IPAddress.Any, 999);
            this.RemoteIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 612);
            this.mainSck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.acceptDone = new ManualResetEvent(false);
            this.KeepServerAlive = true;
        }
        public void AcceptConnect()
        {
            if (this.mainSck == null)
            {
                this.InitProperties();
            }
            this.mainSck.Bind(this.ServerIP);
            this.mainSck.Listen(3000);
            Console.WriteLine("Start Server ... Listen Port:" + this.ServerIP.Port);
            do{
                this.acceptDone.Reset();
                this.mainSck.BeginAccept(this.AcceptCallback, this.mainSck);
                this.acceptDone.WaitOne();
                Console.WriteLine("Next Waiting for ...");
            }
            while(this.KeepServerAlive);
        }
        /// <summary>
        /// async accept a client 
        /// </summary>
        /// <param name="ar">asynchronous</param>
        protected virtual void AcceptCallback(IAsyncResult ar)
        {
            if (ar.IsCompleted)
            {
                Socket main = (Socket)ar.AsyncState;
                using (Socket client = main.EndAccept(ar))
                {
                    this.acceptDone.Set();
                    //來源端逾時
                    client.SendTimeout = 6000;
                    client.ReceiveTimeout = 6000;
                    Console.WriteLine("Remote Clinet: " + client.RemoteEndPoint.ToString());
                    try
                    {
                        byte[] response = null;
                        byte[] request = null;
                        Console.WriteLine("Proxy要連的遠端:" + this.RemoteIP.ToString());
                        //以同步方式=>1.取得來源端資料=>2.連到目的端並交換資料=>3.回送目的端回傳資料
                        if (this.ReceiveData(client, out request) &&
                            this.SocketClient(this.RemoteIP, request, out response) &&
                            this.SendData(client, response))
                        {
                            Console.WriteLine("Response Complete: {0}", BitConverter.ToString(response).Replace("-", ""));
                        }
                        else
                        {
                            Console.WriteLine("Proxy異常: 接收要代理的數據時,發生異常");
                        }


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Connect Error:" + ex.Message);
                    }
                }
            }
        }
        /// <summary>
        /// Socket送出資料
        /// </summary>
        /// <param name="client">Socket</param>
        /// <param name="request">送出請求</param>
        /// <returns></returns>
        protected virtual bool SendData(Socket client,byte[] request)
        {
            int sendLength = 0;
            if(client.Connected){
                    sendLength = client.Send(request, SocketFlags.None);
                    if (sendLength == request.Length)
                    {
                        return true;
                    }
            }
            return false;
        }
        /// <summary>
        /// Socket接收資料
        /// </summary>
        /// <param name="client">Socket</param>
        /// <param name="response">接收的資料</param>
        /// <returns>true:成功接收資料/false:接收資料異常</returns>
        protected virtual bool ReceiveData(Socket client,out byte[] response)
        {
            int receiveLength = 0;
            byte[] buffer = null;
            response = null;
            try
            {
                do
                {
                    buffer = new byte[0x1000];//4k
                    receiveLength = client.Receive(buffer);
                    Array.Resize(ref buffer, receiveLength);
                    if (response == null)
                    {
                        response = buffer;
                    }
                    else
                    {
                        response.Concat(buffer);
                    }
                }
                while (client.Available != 0);

                Console.WriteLine("ReceiveData(length:{0}): {1}", response.Length, BitConverter.ToString(response).Replace("-", ""));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("接收異常:" + ex.Message);
                return false;
            }

        }
        /// <summary>
        /// 連線目的端並送出請求與取得回應
        /// </summary>
        /// <param name="remoteIp">遠端端點資訊</param>
        /// <param name="request">請求資料</param>
        /// <param name="response">回應資料</param>
        /// <returns>true:目的端有回應/false:目的端異常</returns>
        protected virtual bool SocketClient(IPEndPoint remoteIp,byte[] request,out byte[] response)
        {
            response = null;
            Socket client = null;
            try
            {
                using (client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    //目的端逾時
                    client.ReceiveTimeout = 3000;
                    client.SendTimeout = 3000;
                    //connect to destination
                    client.Connect(remoteIp);
                    //send request
                    if (!this.SendData(client, request))
                    {
                        return false;
                    }
                    //get response
                    if (this.ReceiveData(client, out response))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                client = null;
                Console.WriteLine("Proxy連線遠端或送出資料異常[{0}]: {1}", client.RemoteEndPoint.ToString(), ex.Message);
                return false;
            }
        }
        //要載入設定的資訊檔 TODO...
        public void LoadSetting(string url)
        {
            string fullPath = url;
            if (!System.IO.File.Exists(fullPath))
            {
                fullPath = AppDomain.CurrentDomain.BaseDirectory;
                
            }
            XDocument doc = XDocument.Load(url, LoadOptions.None);
            var qq = doc.Root; 
            doc.Elements("");
        }


        //打算依據來源IP來找目的IP列表,並且平行發送訊息,只要有一個有回應就可以了 TODO...
        private IList<IPEndPoint> GetDestinationList(IPEndPoint origin)
        {
            var qwe = this.proxyList.AsParallel().First(x => x.Key.Contains(origin)).Value;
            return qwe;
        }

        public void Dispose()
        {
            if (this.acceptDone != null)
            {
                this.acceptDone.Set();
                this.KeepServerAlive = false;
                if (this.mainSck != null)
                {
                    try
                    {
                        if (this.mainSck.Connected)
                            this.mainSck.Shutdown(SocketShutdown.Both);
                        this.mainSck.Close(100);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Main Socket dispose error: {0}", ex.Message);
                        this.mainSck.Dispose();
                    }
                }
                this.acceptDone.Dispose();
            }
        }
    }
}
