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
    class Test_MDN_Sample
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
        /* Client端 直接用console測試
         * 
         * var url = "ws://xxx.xx.xxx.xx:612";
         * var ws = new WebSocket(url);
           ws.onopen = function(){
                console.log('connected...');
           };
           var receivedata;
           ws.onmessage = function(e){
                receivedata = e.data;
                console.log('接收到:' + receivedata );
           };
           ws.onclose = function(){
                console.log('closed ...');
           };
           ws.onerror = function(){
                console.log('error');
           };
        */
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
            Thread.Sleep(1000);//1sec
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
            Console.Out.WriteLine("已送出HandShaking Header");
            //*****************************************************
           
            Console.ReadKey();
            byte[] buffer2 = new byte[0x1000];
            Console.Out.WriteLine("等待資料");
            int readCnt2 = ns.Read(buffer2, 0, buffer2.Length);//會Block  等待client send msg
            Array.Resize(ref buffer2,readCnt2);
            byte[] clientData = DecryptClientData(buffer2);
            string readData2 = Encoding.UTF8.GetString(clientData, 0, clientData.Length);
            Console.Out.WriteLine("收到的資料:{0}", readData2);
            //*****************************************************
            Console.ReadKey();
            //ref: https://tools.ietf.org/html/rfc6455#section-5.1
            //ref2:http://limitedcode.blogspot.tw/2014/05/websocket-websocket-server-console.html
            string sendMsg = "Test123";//TODO...Server送出到前端WebSocket失敗
            byte[] sendMsgBytes = Encoding.UTF8.GetBytes(sendMsg);
            List<byte> dataSend = new List<byte>();
            dataSend.Add(0x81);//data < 126 
            dataSend.Add((byte)sendMsgBytes.Length);//data length
            dataSend.AddRange(sendMsgBytes);//data content
            ns.Write(dataSend.ToArray(), 0, dataSend.Count);
            Console.WriteLine("送出伺服端的Msg:{0}", sendMsg);
            Console.ReadKey();
            /*****************************************************************/
            Console.Out.WriteLine("按任意鍵結束 ...");
            Console.ReadKey();
            ns.Close();
            Console.Out.WriteLine("GG...............");
            Console.ReadKey();
        }
        //Server端送出Msg作格式處理// 看起來不用做XOR -2016-01-29
        static byte[] DoXor(byte[] data,byte[] key)
        {
            byte[] result = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ key[i % key.Length]);
            }

            return result;
        }
        //Server端送出資料串接
        static byte[] ConBineData(byte fin, byte dataLength, byte[] key, byte[] data)
        {
            byte[] result = new byte[1 + 1 + key.Length + data.Length];
            result[0] = fin;
            result[1] = dataLength;
            //湊Key
            for (int i = 2; i < key.Length + 2; i++)
            {
                result[i] = key[i - 2];
            }
            //湊資料段
            for (int j = (2 + key.Length); j < (data.Length + key.Length + 2); j++)
            {
                result[j] = data[j - (2 + key.Length)];
            }
            return result;
        }
        //WebSocket那邊送的資料轉回來
        static byte[] DecryptClientData(byte[] data)
        {
            byte fin = data[0];
            int dataLength = (data[1] - 128);
            //copy key
            byte[] key = new byte[4];
            Buffer.BlockCopy(data, 2, key, 0, key.Length);
            //copy data
            byte[] encData = new byte[dataLength];
            Buffer.BlockCopy(data, (1 + 1 + key.Length), encData, 0, encData.Length);
            byte[] result = new byte[encData.Length];
            for(int i = 0; i < encData.Length;i++)
            {
                result[i] = (byte)(encData[i] ^ key[i % key.Length]);
            }

            return result;
        }
    }

}
