using System;
//
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Security.Cryptography;

namespace WebSocket_Test
{
    //ref:http://stackoverflow.com/questions/10200910/create-hello-world-websocket-example
    public class Test_WebSocket
    {
        static private string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        static void Main(string[] args)
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 611));
            serverSocket.Listen(128);
            BeginAcceptRequest(serverSocket);
            Console.WriteLine("開始非同步監聽...");
            ConsoleKey exit = ConsoleKey.N;
            while (exit != ConsoleKey.Y)
            {
                exit = Console.ReadKey().Key;
            }
        }

        static void BeginAcceptRequest(Socket main)
        {
            try
            {
                main.BeginAccept(null, 0, AcceptCallback, main);
                ShowTthreadPoolInfo();
            }
            catch (SocketException ex)
            {
                Console.WriteLine("[BeginAcceptRequest][ThreadId:{0}]Error:{1}", Thread.CurrentThread.ManagedThreadId, ex.Message);
            }
        }
        private static void AcceptCallback(IAsyncResult result)
        {
            byte[] buffer = new byte[1024];

            Socket client = null;
            try
            {
                Socket serverSocket = result.AsyncState as Socket;
                string headerResponse = "";
                if (serverSocket != null && serverSocket.IsBound)
                {
                    BeginAcceptRequest(serverSocket);
                    client = serverSocket.EndAccept(result);
                    ShowTthreadPoolInfo();
                    Console.WriteLine("start receive data ...");
                    var i = client.Receive(buffer);
                    Console.WriteLine("end receive data ...");
                    headerResponse = (System.Text.Encoding.UTF8.GetString(buffer)).Substring(0, i);
                    // write received data to the console
                    Console.WriteLine(headerResponse);

                }
                if (client != null)
                {
                    /* Handshaking and managing ClientSocket */

                    //var key = headerResponse.Replace("key:", "`")
                    //          .Split('`')[1]                     // dGhlIHNhbXBsZSBub25jZQ== \r\n .......
                    //          .Replace("\r", "").Split('\n')[0]  // dGhlIHNhbXBsZSBub25jZQ==
                    //          .Trim();
                    var key = headerResponse + " Test";
                    // key should now equal dGhlIHNhbXBsZSBub25jZQ==
                    var test1 = AcceptKey(ref key);

                    var newLine = "\r\n"; //Environment.NewLine;

                    var response = "HTTP/1.1 101 Switching Protocols" + Environment.NewLine//newLine
                         + "Upgrade: websocket" + newLine
                         + "Connection: Upgrade" + newLine
                         + "Sec-WebSocket-Accept: " + test1 + newLine + newLine
                        //+ "Sec-WebSocket-Protocol: chat, superchat" + newLine
                        //+ "Sec-WebSocket-Version: 13" + newLine
                         ;

                    // which one should I use? none of them fires the onopen method
                    client.Send(System.Text.Encoding.UTF8.GetBytes(response));

                    var i = client.Receive(buffer); // wait for client to send a message

                    // once the message is received decode it in different formats
                    Console.WriteLine(Convert.ToBase64String(buffer).Substring(0, i));

                    Console.WriteLine("\n\nPress enter to send data to client");
                    Console.Read();

                    var subA = SubArray<byte>(buffer, 0, i);
                    client.Send(subA);
                    Thread.Sleep(10000);//wait for message to be send


                }
            }
            catch (SocketException exception)
            {
                Console.WriteLine("Socket異常:{0}",exception.Message);
                client.Close();
                client= null;
                Console.WriteLine("關閉此次Client");
            }
        }

        private static void ShowTthreadPoolInfo()
        {
            int workerThreads = 0;
            int completionThreads = 0;
            ThreadPool.GetAvailableThreads(out workerThreads, out completionThreads);
            Console.WriteLine("{0} 目前背景執行緒數量:{1}, 非同步IO數量:{2}", GetMethodName(2), workerThreads, completionThreads);
        }

        private static string GetMethodName(int index = 1)
        {
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
            return "[" + st.GetFrame(index).GetMethod().Name + "]";
        }

        public static T[] SubArray<T>(T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        private static string AcceptKey(ref string key)
        {
            string longKey = key + guid;
            byte[] hashBytes = ComputeHash(longKey);
            return Convert.ToBase64String(hashBytes);
        }

        static SHA1 sha1 = SHA1CryptoServiceProvider.Create();
        private static byte[] ComputeHash(string str)
        {
            return sha1.ComputeHash(System.Text.Encoding.ASCII.GetBytes(str));
        }
    }
}
