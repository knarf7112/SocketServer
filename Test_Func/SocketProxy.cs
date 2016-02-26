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
    public delegate void WriteLog(Enum logLevel,string Msg);
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
        /// <summary>
        /// listen port
        /// </summary>
        private int ListenPort { get; set; }

        private IDictionary<IList<IPEndPoint>, IList<IPEndPoint>> proxyList;
        /// <summary>
        /// 委派給外部指定寫log的方法指標,若無指定就用Console寫
        /// </summary>
        public WriteLog OnWriteLog { get; set; }

        public SocketProxy(int listenPort)
        {
            this.ListenPort = listenPort;
            this.Init();
        }


        private void Init()
        {
            this.ServerIP = new IPEndPoint(IPAddress.Any, this.ListenPort);
            this.mainSck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.acceptDone = new ManualResetEvent(false);
            this.KeepServerAlive = true;
            string path = AppDomain.CurrentDomain.BaseDirectory + @"\ProxyIPSettings.xml";
            this.proxyList = this.LoadSettings(path);
            //會先用HostName尋問本機端的hosts列表,若查無對應IP
            //再連到本機設定的Dns尋問,用hostName來嘗試取得IP,若無法解析hostName會拋異常(即查無任何對應IP)
            //IPAddress[] IPs = Dns.GetHostEntry(IPAddress.Parse(remoteIP)).AddressList;
            //foreach (IPAddress ip in IPs)
            //{
            //    Console.WriteLine("Dns Resolve's IPAddress: {0}", ip.ToString());
            //}
        }

        /// <summary>
        /// 背景啟動Proxy
        /// </summary>
        public void Start()
        {
            Task.Run(()=>{
                this.Run();
            });
        }

        /// <summary>
        /// 啟動Proxy
        /// </summary>
        protected virtual void Run()
        {
            if (this.mainSck == null)
            {
                this.Init();
            }
            this.mainSck.Bind(this.ServerIP);
            this.mainSck.Listen(3000);
            this.WriteLog(LogLevel.DEBUG,"Start Server ... Listen Port:" + this.ServerIP.Port);
            do{
                this.acceptDone.Reset();
                this.mainSck.BeginAccept(this.AcceptCallback, this.mainSck);
                this.acceptDone.WaitOne();
                Console.WriteLine("Accept a client ...");
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
                    IList<IPEndPoint> destinationList = null;
                    Console.WriteLine("Origin IP: " + client.RemoteEndPoint.ToString());
                    try
                    {
                        byte[] response = null;
                        byte[] request = null;
                        destinationList = GetDestinationList((IPEndPoint)client.RemoteEndPoint);
                        //destinationList = GetDestinationList(((IPEndPoint)client.RemoteEndPoint).Address.ToString()).ToList();
                        if (destinationList == null)
                        {
                            throw new Exception("[來源:" + client.RemoteEndPoint.ToString() + "]字典內查無遠端設定");
                        }
                        //Console.WriteLine("Proxy要連的遠端:" + this.RemoteIP.ToString());
                        //以同步方式=>1.取得來源端資料=>2.連到目的端並交換資料=>3.回送目的端回傳資料
                        if (this.ReceiveData(client, out request) &&
                            this.GetResponse(destinationList, request, out response) &&
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
        /// 用Socket送出request資料至指定的server並取得response
        /// </summary>
        /// <param name="remoteIp">遠端端點資訊</param>
        /// <param name="request">請求資料</param>
        /// <param name="response">回應資料</param>
        /// <param name="sendTimeout">default: 3 second</param>
        /// <param name="receiveTimeout">default: 3 second</param>
        /// <returns>true:目的端有回應/false:目的端異常</returns>
        protected virtual bool GetResponse(IPEndPoint remoteIp, byte[] request, out byte[] response, int sendTimeout = 3000, int receiveTimeout = 30000)
        {
            response = null;
            Socket client = null;
            try
            {
                using (client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    //目的端逾時
                    client.ReceiveTimeout = sendTimeout;
                    client.SendTimeout = receiveTimeout;
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

        protected virtual void WriteLog(Enum logLevel,string msg)
        {
            if (this.OnWriteLog != null)
            {
                this.OnWriteLog.Invoke(logLevel, msg);
            }
            else
            {
                Console.WriteLine(msg);
            }
        }

        #region 取得遠端列表的(全部/任一)回應
        /// <summary>
        /// 用Socket取得單一遠端的回應
        /// </summary>
        /// <param name="remoteIp">指定的server端設定</param>
        /// <param name="request">request data</param>
        /// <param name="sendTimeout">default: 3 second</param>
        /// <param name="receiveTimeout">default: 3 second</param>
        /// <returns>response data</returns>
        protected virtual byte[] GetResponse(IPEndPoint remoteIp, byte[] request,int sendTimeout = 3000,int receiveTimeout = 30000)
        {
            byte[] response = null;
            Socket client = null;
            try
            {
                Console.WriteLine("Destination IP:{0}", remoteIp.ToString());
                using (client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    //目的端逾時
                    client.ReceiveTimeout = sendTimeout;
                    client.SendTimeout = receiveTimeout;
                    //connect to destination
                    client.Connect(remoteIp);
                    //send request
                    this.SendData(client, request);
                    //get response
                    this.ReceiveData(client, out response);
                    return response;
                }
            }
            catch (Exception ex)
            {
                client = null; 
                Console.WriteLine("Proxy連線遠端或送出資料異常[{0}]: {1}", client.RemoteEndPoint.ToString(), ex.Message);
                return response;
            }
        }

        /// <summary>
        /// 用Socket取得遠端列表的(全部/任一)回應
        /// </summary>
        /// <param name="remoteIpList">遠端目的列表</param>
        /// <param name="request">request data</param>
        /// <param name="response">response data</param>
        /// <param name="waitAll">是否等待所有遠端response</param>
        /// <param name="sendTimeout">default: 3 second</param>
        /// <param name="receiveTimeout">default: 3 second</param>
        /// <returns>true:取得response/false:遠端連線異常</returns>
        protected virtual bool GetResponse(IList<IPEndPoint> remoteIpList, byte[] request, out byte[] response,bool waitAll = false, int sendTimeout = 3000, int receiveTimeout = 30000)
        {
            Queue<Task<byte[]>> resultList = new Queue<Task<byte[]>>();
            response = null;
            try
            {
                foreach (IPEndPoint dest in remoteIpList)
                {
                    //task run
                    Task<byte[]> task = Task.Run(() => { return GetResponse(dest, request, sendTimeout, receiveTimeout); });
                    //push in task list
                    resultList.Enqueue(task);
                }
                if (waitAll)
                {
                    bool complete = Task.WaitAll(resultList.ToArray(), 50000);//timeout: 50 second
                    //if need compare all result to do that// if need TODO...
                    //resultList.GroupBy(n => n.Result)....;

                    //if complete filter result is null and get the first task result
                    response = complete ? resultList.Where(n => n.Result != null).FirstOrDefault().Result : null;
                    return complete;

                }
                else
                {
                    int index = Task.WaitAny(resultList.ToArray(), 50000);//tiemout: 50 second
                    //if anyone is complete use the return index to find task result
                    response = index.Equals(-1) ? null : resultList.ElementAtOrDefault(index).Result;
                    return !index.Equals(-1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[GetResponse]Error:" + ex.Message + Environment.NewLine + ex.StackTrace);
                return false;
            }
        }
        #endregion

        #region Load Xml File to settings
        /// <summary>
        /// 讀取指定路徑的Xml設定檔,轉成來源端列表與目的端列表的字典檔
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public IDictionary<IList<IPEndPoint>, IList<IPEndPoint>> LoadSettings(string filePath)
        {
            IDictionary<IList<IPEndPoint>, IList<IPEndPoint>> ipEndPointDic = new ConcurrentDictionary<IList<IPEndPoint>, IList<IPEndPoint>>();
            //load xml file
            XDocument doc = XDocument.Load(filePath, LoadOptions.None);

            IEnumerable<XElement> ipEndPointList = doc.Root.Elements("IPEndPoint");
            IEnumerable<XElement> originNodes = null;
            IEnumerable<XElement> destinationNodes = null;
            //loop all Tag that name is IPEndPoint
            foreach (var ipEndPointNode in ipEndPointList)
            {
                if (ipEndPointNode.HasElements)
                {
                    //get this endpoint inner settings
                    //loading origin ip list setting
                    originNodes = ipEndPointNode.Elements("Origin");
                    //loading destination ip list setting
                    destinationNodes = ipEndPointNode.Elements("Destination");
                    //parse origin ip list setting
                    IList<IPEndPoint> originlist = GetIpEndPointList(originNodes, "Origin");
                    //parse destination ip list setting
                    IList<IPEndPoint> destinationlist = GetIpEndPointList(destinationNodes, "Destination");
                    //insert to dictionary
                    ipEndPointDic.Add(originlist, destinationlist);
                }
            }
            return ipEndPointDic;
        }
        
        /// <summary>
        /// 將指定的XML項目列表轉成IPEndPoint物件列表
        /// </summary>
        /// <param name="firstNodeGroups">內含IP和Port標籤的Xml群組元素</param>
        /// <param name="groupName">群組名稱</param>
        /// <returns>IPEndPoint列表</returns>
        protected virtual IList<IPEndPoint> GetIpEndPointList(IEnumerable<XElement> firstNodeGroups, string groupName)
        {
            IList<IPEndPoint> list = new List<IPEndPoint>();
            IPEndPoint UrlInfo = null;
            if (firstNodeGroups.Count() == 0)
            {
                throw new InvalidOperationException(groupName + "無設定資料");
            }
            //第一層(List)
            foreach (var node in firstNodeGroups)
            {

                IEnumerable<XElement> childNodes = node.Elements();
                string ip = string.Empty;
                int port = -1;
                //第二層(IPEndPoint)
                foreach (XElement item in childNodes)
                {
                    switch (item.Name.LocalName.ToUpper())
                    {
                        case "IP":
                            //Any IP or specified IP
                            ip = (item.Value == "*") ? IPAddress.Any.ToString() : item.Value;
                            break;
                        case "PORT":
                            //Any Port or specified Port
                            port = (item.Value == "*") ? 0 : Int32.Parse(item.Value);
                            break;
                        case "SENDTIMEOUT":
                        case "RECEIVETIMEOUT":
                        default:
                            break;
                    }
                }
                //valid ip and port
                if (!string.IsNullOrEmpty(ip) && !port.Equals(-1))
                {
                    UrlInfo = new IPEndPoint(IPAddress.Parse(ip), port);
                    list.Add(UrlInfo);
                }
            }
            return list;
        }

        /// <summary>
        /// 依據來源IPEndPoint來找設定檔內的目的地列表,無則為null
        /// </summary>
        /// <param name="origin">來源IP資訊</param>
        /// <returns>目的地IP資訊列表</returns>
        protected IList<IPEndPoint> GetDestinationList(IPEndPoint origin)
        {
             return this.proxyList.FirstOrDefault(n => n.Key.Contains(origin)).Value;
        }
        /// <summary>
        /// 列舉所有符合來源IP和Port所對應的遠端資訊
        /// </summary>
        /// <param name="dicProxySetting">載入設定的字典檔</param>
        /// <param name="origin">來源IPEndPoint</param>
        /// <returns>所有符合設定檔的遠端</returns>
        protected static IEnumerable<IPEndPoint> GetDestinationList(IDictionary<IList<IPEndPoint>, IList<IPEndPoint>> dicProxySetting, IPEndPoint origin)
        {
            foreach (IList<IPEndPoint> proxy in dicProxySetting.Keys)
            {
                //來源連線資訊符合字典集合內的條件
                //條件1.指定來源的IP和Port都一樣
                if (proxy.Any(m => ((m.Equals(origin)) ||
                    //條件2.指定來源的IP符合允許任何Port
                             (m.Address.Equals(origin.Address) && m.Port.Equals(0)) ||
                    //條件3.指定來源為允許任何IP但Port要符合
                             (m.Address.Equals(IPAddress.Any) && m.Port.Equals(origin.Port)) ||
                    //條件4.允許任何IP且允許任何Port
                             (m.Address.Equals(IPAddress.Any) && m.Port.Equals(0)))))
                {
                    foreach (var item in dicProxySetting[proxy])
                    {
                        yield return item;
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// dispose proxy server
        /// </summary>
        public void Dispose()
        {
            if (this.acceptDone != null)
            {
                this.OnWriteLog = null;
                this.acceptDone.Set();//release blocking thread
                this.KeepServerAlive = false;// stop loop
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
    //reference log4net Log Level
    enum LogLevel
    {
        ALL,
        DEBUG,
        INFO,
        WARN,
        ERROR,
        FATAL,
        OFF,
    }
}
