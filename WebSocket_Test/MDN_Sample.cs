using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//
using System.Net.Sockets;
using System.Net;
using System.Threading;
//
using System.Text.RegularExpressions;
using System.Security.Cryptography;


namespace WebSocket_Test
{
    class MDN_Sample
    {
        //*****************************************************************************
        static void Main1(string[] args)
        {
            Timer t = new Timer(TimerCallback, null, 0, 2000);


            Console.ReadLine();
            //var qq = t;
        }

        static void TimerCallback(object o)
        {
            Console.WriteLine("In Timer Callback" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            GC.Collect();//註解掉就會繼續跑
            //強制執行GC後 Timer就只會跑第一次,因為t變數在Main方法中已不再被執行 ,所以GC回收掉t,
            //但是如果我在下面加一個參考,雖然Block在ReadLine那邊,但因為仍被參考,所以t不會被GC當垃圾丟掉
        }
        //****************************************************************************


        //****************************************************************************
        //ref:https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_server
        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(IPAddress.Any, 612);

            server.Start();
            Console.WriteLine("Server has started on 127.0.0.1:80.{0}Waiting for a connection...", Environment.NewLine);

            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("A client connected.");
            NetworkStream ns =  client.GetStream();//cast

            //Queue<byte> buffer = new Queue<byte>();
            //int data = -1;
            while (!ns.DataAvailable) ;
            //*****************************************************
            //while ((data = ns.ReadByte()) > -1)
            //{
            //    buffer.Enqueue((byte)data);
            //}
            //不知為啥轉不完  神奇的不會往下執行
            //*****************************************************
            byte[] buffer = new byte[0x1000];
            Console.Out.WriteLine("等待資料");
            int readCnt = ns.Read(buffer, 0, buffer.Length);
            string readData = Encoding.UTF8.GetString(buffer, 0, readCnt);
            Console.Out.WriteLine("收到的資料:{0}", readData);
            //string readData = Encoding.UTF8.GetString(buffer.ToArray());
            //Console.Out.WriteLine("接收到的資料:{0}", readData);
            //*****************************************************
            Thread.Sleep(20000);//20sec
            byte[] response = null;
            //ref:https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_server
            if (new Regex("^GET").IsMatch(readData))
            {
                response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                                                + "Connection: Upgrade" + Environment.NewLine
                                                + "Upgrade: websocket" + Environment.NewLine
                                                + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                                                SHA1.Create().ComputeHash(
                                                Encoding.UTF8.GetBytes(
                                                new Regex("Sec-WebSocket-Key: (.*)").Match(readData).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))) 
                                                + Environment.NewLine + Environment.NewLine);

            }
            byte[] sendData = null;//Encoding.UTF8.GetBytes("Test123 haha");
            //ref: https://developer.mozilla.org/zh-TW/docs/WebSockets/Writing_WebSocket_client_applications
            sendData = response;
            ns.Write(sendData, 0, sendData.Length);
            Console.Out.WriteLine("送出的資料:Test123 haha");
            //*****************************************************
            Console.ReadKey();
            byte[] buffer2 = new byte[0x1000];
            Console.Out.WriteLine("等待資料");
            int readCnt2 = ns.Read(buffer2, 0, buffer2.Length);//不會Block  原因目前不明
            string readData2 = Encoding.UTF8.GetString(buffer2, 0, readCnt2);
            Console.Out.WriteLine("收到的資料:{0}", readData2);
            
            ns.Close();
            Console.Out.WriteLine("GG...............");
            Console.ReadKey();
        }
    }
}
