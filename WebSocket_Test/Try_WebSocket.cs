using System;
//
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Security.Cryptography;

namespace WebSocket_Test
{
    //ref:http://stackoverflow.com/questions/10200910/create-hello-world-websocket-example
    public class Try_WebSocket
    {
        static Socket serverSocket = new Socket(AddressFamily.InterNetwork,
        SocketType.Stream, ProtocolType.Tcp);
        static private string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        static void Main1(string[] args)
        {
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 611));
            serverSocket.Listen(128);
            serverSocket.BeginAccept(null, 0, OnAccept, null);
            Console.WriteLine("開始非同步監聽...");
            ConsoleKey exit = ConsoleKey.N;
            while (exit != ConsoleKey.Y)
            {
                exit = Console.ReadKey().Key;
            }
        }

        private static void OnAccept(IAsyncResult result)
        {
            byte[] buffer = new byte[1024];
            Socket client = null;
            try
            {
               
                string headerResponse = "";
                if (serverSocket != null && serverSocket.IsBound)
                {
                    
                    client = serverSocket.EndAccept(result);
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

                    var response = "HTTP/1.1 101 Switching Protocols" + newLine
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
            finally
            {
                if (serverSocket != null && serverSocket.IsBound)
                {
                    serverSocket.BeginAccept(null, 0, OnAccept, null);
                }
            }
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
