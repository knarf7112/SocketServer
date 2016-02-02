using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//
using System.Net;

namespace Test_Func
{
    public class SocketProxy : IDisposable
    {
        private Socket mainSck;

        private ManualResetEvent acceptDone;

        private bool KeepServerAlive;

        private IPEndPoint RemoteIP { get; set; }
        private IPEndPoint ServerIP { get; set; }
        public SocketProxy()
        {
            this.InitProperties();
        }

        public SocketProxy(int listenPort, string remoteIP, int remotePort)
        {
            //解析遠端,無法解析會拋異常
            IPAddress[] IPs = Dns.GetHostEntry(IPAddress.Parse(remoteIP)).AddressList;
            this.RemoteIP = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);
            this.ServerIP = new IPEndPoint(IPAddress.Any, listenPort);
            this.mainSck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.acceptDone = new ManualResetEvent(false);
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

        protected virtual void AcceptCallback(IAsyncResult ar)
        {
            if (ar.IsCompleted)
            {
                Socket main = (Socket)ar.AsyncState;
                using (Socket client = main.EndAccept(ar))
                {
                    this.acceptDone.Set();
                    Console.WriteLine("Remote Clinet: " + client.RemoteEndPoint.ToString());
                    try
                    {
                        byte[] response = null;
                        byte[] request = null;
                        Console.WriteLine("Proxy要連的遠端:" + this.RemoteIP.ToString());
                        if (this.ReceiveData(client, out request) &&
                            this.SocketClient(this.RemoteIP, request, out response) &&
                            this.SendData(client, response))
                        {
                            Console.WriteLine("ProxyClient Response: {0}", BitConverter.ToString(response).Replace("-", ""));
                        }
                        else
                        {
                            Console.WriteLine("ProxyClient異常: 接收要代理的數據時,發生異常");
                        }


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Connect Error:" + ex.Message);
                    }
                }
            }
        }

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

        protected virtual bool SocketClient(IPEndPoint remoteIp,byte[] request,out byte[] response)
        {
            response = null;
            Socket client = null;
            try
            {
                using (client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    client.ReceiveTimeout = 6000;
                    client.SendTimeout = 6000;
                    client.Connect(remoteIp);
                    if (!this.SendData(client, request))
                    {
                        return false;
                    }
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
                Console.WriteLine("連線或送出異常:" + ex.Message);
                return false;
            }
        }

        public void Dispose()
        {
            
        }
    }
}
