using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Net.Sockets;
using System.Net;
//
using System.Collections.Concurrent;
//thread
using System.Threading;

namespace Test_Func.Test
{
    public delegate void Exec_Commad(string cmd);
    public class Test_AsyncSocket
    {
        public Socket main;
        public ConcurrentBag<Socket> clients;

        public Exec_Commad OnExec_Commad { get; set; }
        public void Init(int port)
        {
            clients = new ConcurrentBag<Socket>();
            main = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            main.Bind(new IPEndPoint(IPAddress.Any, port));
            main.Listen(5000);
            
        }

        public void Start()
        {
            try{
                Console.WriteLine("[Start]Current ThreadId:{0}", Thread.CurrentThread.ManagedThreadId);
                main.BeginAccept(new AsyncCallback(AcceptCallback), main);
                Console.WriteLine("[Start] one client waiting for ...");
            }
            catch (SocketException sckEx)
            {
                Console.WriteLine("[Start]Socket Error: {0}", sckEx.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Start]Error: {0}", ex.Message);
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                if (ar.IsCompleted)
                {
                    Socket client = ((Socket)ar.AsyncState).EndAccept(ar);
                    Console.WriteLine("[AcceptCallback]");
                    clients.Add(client);
                }
            }
            catch (SocketException sckEx)
            {
                Console.WriteLine("[AcceptCallback] Socket Error: {0}", sckEx.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AcceptCallback] Error: {0}", ex.Message);
            }

        }

        
    }

}
