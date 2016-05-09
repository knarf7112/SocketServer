using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Collections.Concurrent;
//
using System.Net.Sockets;
//
using System.Net;
using System.Threading;

namespace Test_Func.Test
{
    public class Client
    {
        public Socket sck { get; set; }
        public int ClientNo { get; set; }
    }

    public class ClientSocket : IDisposable
    {
        public ConcurrentBag<Client> Clients { get; private set; }
        private static Object lockObj = new Object();
        public int Connected_Count;
        public EndPoint RemoteEndPoint { get; private set; }
        public ClientSocket(string ip, int port)
        {
            this.Clients = new ConcurrentBag<Client>();

            RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public void InitSocketClient(int count)
        {
            for(int i = 0;i < count;i++)
            {
                Socket sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Client client = new Client { sck = sck, ClientNo = (i + 1) };
                Clients.Add(client);
            }
        }

        public void StartConnect_All()
        {
            WaitHandle[] allAsync = new WaitHandle[Clients.Count];
            foreach (Client client in Clients)
            {
                if (null != client.sck)
                   allAsync[client.ClientNo - 1] =  Connect(client);
            }
            if (WaitHandle.WaitAll(allAsync))
                Console.WriteLine("完成Client連線: 目前連線總數:{0}", this.Connected_Count);
        }

        public WaitHandle Connect(Client client)
        {
            WaitHandle waithandler = null;
            try
            {
                PrintThreadPool("[Before Connect]");
                IAsyncResult ar = client.sck.BeginConnect(this.RemoteEndPoint, ConnectCallback, client);
                PrintThreadPool("[After Connect]");
                waithandler =  ar.AsyncWaitHandle;//無效
            }
            catch (SocketException sckEx)
            {
                Console.WriteLine("[Connect]Socket error:{0}", sckEx.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Connect]error:{0}", ex.Message);
            }
            return waithandler;
        }
        public void PrintThreadPool(string str)
        {
            int workThread = 0;
            int completeThread = 0;
            ThreadPool.GetAvailableThreads(out workThread, out completeThread);
            Console.WriteLine("{0} WorkThread:{1}, CompleteThread:{2}", str, workThread, completeThread);
        }
        public void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                if (ar.IsCompleted)
                {
                    Client client = ar.AsyncState as Client;
                    client.sck.EndConnect(ar);
                    Console.WriteLine("[ConnectCallback]完成連線 ClientNo:{0} ThreadId{1}", client.ClientNo, Thread.CurrentThread.ManagedThreadId);
                    lock (lockObj)
                    {
                        Connected_Count += 1;
                    }
                }
                else
                {
                    Console.WriteLine("[ConnectCallback]非同步連線未完成 ThreadId:{0}", Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (SocketException sckEx)
            {
                Console.WriteLine("[ConnectCallback]Socket error:{0}", sckEx.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ConnectCallback]error:{0}", ex.Message);
            }

        }

        public void CloseAll()
        {
            foreach (Client client in this.Clients)
            {
                try
                {
                    client.sck.Shutdown(SocketShutdown.Both);
                    client.sck.Close();
                }
                catch (SocketException sckEx)
                {
                    Console.WriteLine("[CloseAll]Socket {0} error:{1}",client.ClientNo, sckEx.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[CloseAll]{0} error:{1}",client.ClientNo, ex.Message);
                }
                finally
                {
                    client.sck = null;
                    //client = null;
                }
            }
            
        }

        public void Dispose()
        {
            CloseAll();
        }
    }
}
