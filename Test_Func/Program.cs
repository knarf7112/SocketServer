using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//
using System.ComponentModel;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace Test_Func
{
    class Program
    {
        //BeginXXX的方式測試AsyncMultiSocketServer.v2的執行檔
        static void Main()
        {
            //Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"),6112);
            Console.WriteLine("開始測試連線");
            for(var i = 0; i < 500; i++){
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                stateObj clientState = new stateObj() { mainsck = client, send = "012340332" + i.ToString("D4") };
                client.BeginConnect(ep, ConnectCallBack, clientState);
            }
            
            Console.ReadKey();
        }
        static void ConnectCallBack(IAsyncResult ar)
        {
            if (ar.IsCompleted)
            {
                stateObj tmp = (stateObj)ar.AsyncState;
                tmp.mainsck.EndConnect(ar);
                Console.WriteLine("Callback : {0}", tmp.send);
                byte[] senddata = Encoding.ASCII.GetBytes(tmp.send);
                tmp.mainsck.BeginSend(senddata, 0, senddata.Length, SocketFlags.None, SendCallback, tmp);
            }
            else
            {
                Console.WriteLine("非同步連線未完成");
            }
        }
        static void SendCallback(IAsyncResult ar)
        {
            if (ar.IsCompleted)
            {
                stateObj tmp = (stateObj)ar.AsyncState;
                int sendLength = tmp.mainsck.EndSend(ar);
                Console.WriteLine("非同步送出Msg length:{0}", sendLength);

                tmp.mainsck.BeginReceive(tmp.buffer, 0, tmp.buffer.Length, SocketFlags.None, ReceiveCallback, tmp);
            }
            else
            {
                Console.WriteLine("非同步送出未完成");
            }
        }

        static void ReceiveCallback(IAsyncResult ar)
        {
            if (ar.IsCompleted)
            {
                stateObj tmp = (stateObj)ar.AsyncState;
                int receiveLength = tmp.mainsck.EndReceive(ar);
                string data = Encoding.ASCII.GetString(tmp.buffer, 0, receiveLength);
                Console.WriteLine("非同步接收Msg length:{0} data:{1}", receiveLength, data);
                Console.WriteLine("開始關閉連線:{0}", tmp.send);
                tmp.mainsck.Close();
            }
            else
            {
                Console.WriteLine("非同步送出未完成");
            }
        }
        class stateObj
        {
            public Socket mainsck;
            public string send;
            public string receive;
            public byte[] buffer = new byte[0x1000];
        } 
        /*********************************************************************************************/
        //TODO: 使用TODO list來提醒未完成工作 -->檢視-->工作清單-->下拉選單選註解-->會出現TODO相關的所有列表
        static void Main6()
        {
            Console.WriteLine("test123\r\nQQQQQ");
            Console.WriteLine("test123{0}789QQQQQ",Environment.NewLine);
            Console.WriteLine("Enum:" + testType.N1.ToString());
            Console.WriteLine("Enum:" +  testType.N1.GetType().Attributes.ToString());
            var test = testType.N1.GetType().GetProperty("N1");
            //取不到Attribute內的Description的內容--TODO...
            var str = DescriptionAttribute.GetCustomAttribute(testType.N1.GetType(), typeof(testType)).ToString();
            
            Debugger.Break();
            Console.ReadKey();
        }

        enum testType
        {
            /// <summary>
            /// 123546
            /// </summary>
            [Description("屬性一的敘述")]
            N1,
            [Description("屬性二的敘述")]
            N2 = 3
        }

        //測試static共用於整個namespace下的那個類別
        static void Main5()
        {
            ClsTest2 t1 = new ClsTest2();
            var length = 10;
            for (int i = 0; i < length; i++)
            {
                t1.Add(i);
            }
            Debugger.Break();
            ClsTest2 t2 = new ClsTest2();
            Debugger.Break();
            Console.ReadKey();


        }

        //測試沒lock時的multi thread來增加數據到List內,假設同時新增數據會不會衝突,不過測試起來都沒出現異常,可能寫入數據太小
        static void Main4()
        {
            Class1 c1 = new Class1();
            Class1 c2 = new Class1();
            int count = 2000;
            Task[] tasks = new Task[count];
            Task[] tasks2 = new Task[count];
            for (int i = 0; i < count; i++)
            {
                var i2 = i;
                tasks2[i] = Task.Run(() =>
                {
                    Console.WriteLine("task2:" + i2);
                    c1.add(i2);
                });
                tasks[i] = Task.Run(() =>
                {
                    Console.WriteLine("task:" + i2);
                    c2.add(i2);
                });
                
            }
            Task.WaitAll(tasks.Concat( tasks2).ToArray());
            StaticClass1.DisplayCount();
            Console.ReadKey();
        }

        static void Main3(string[] args)
        {
            Console.WriteLine("Main:" + Thread.CurrentThread.ManagedThreadId);
            for (var i = 0; i < 5; i++)
            {
                var qq = getAsync2("http://www.google.com");
                qq.Wait();
                Console.WriteLine("TaskScheduler" + TaskScheduler.Current.Id);
            }
            Console.ReadKey();
        }

        async static Task getAsync(string url)
        {
            Console.WriteLine("getAsync:" + Thread.CurrentThread.ManagedThreadId);
            WebClient client = new WebClient();
            var task = Task.Run(() =>
            {
                Console.WriteLine("Run:" + Thread.CurrentThread.ManagedThreadId);
                Thread.Sleep(5000);
                string content = client.DownloadString(url);
                Console.WriteLine("網頁長度: " + content.Length);
            });
            await task;
        }

        async static Task getAsync2(string url)
        {
            Console.WriteLine("TaskScheduler" + TaskScheduler.Current.Id);
            Console.WriteLine("getAsync2:" + Thread.CurrentThread.ManagedThreadId);
            WebClient client = new WebClient();
            Thread.Sleep(1000);
            string content = await client.DownloadStringTaskAsync(url);
            Console.WriteLine("Run2:" + Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine("網頁長度: " + content.Length);
        }

        static void Main2(string[] arg)
        {
            //var qq = SynchronizationContext.Current;
            //Socket sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //TaskFactory d;
            //sck.BeginAccept()
            Program p = new Program();
            for (var i = 0; i < 30; i++)
            {
                Random rnd = new Random(i);
                int r = rnd.Next(0, 3000);
                qq ttt = new qq() { i = i, r = r };
                //Task.Factory.StartNew<qq>( (object obj,qq t)=>{
                //    Thread.Sleep(3000 - t.i * 100);
                //    Console.WriteLine("Asynchronization M1:" + t.i + ": sleep:" + t.r);
                //    t.i = t.i + 1;
                //    return t;
                //},ttt);
                //p.M1(ttt).ContinueWith(iii =>
                //{
                //    Console.WriteLine("continue:" + ((qq)iii.Result).i + ": sleep:" + ((qq)iii.Result).r);
                //});
            }
            Console.ReadKey();
        }

        async Task<qq> M1(qq t)
        {
            Thread.Sleep(3000 - t.i * 100);
            Console.WriteLine("Asynchronization M1:" + t.i + ": sleep:" + t.r);
            t.i = t.i + 1;
            return t;
        }
        class qq
        {
            public int i { get; set; }
            public int r { get; set; }
        }
        static void Main1(string[] args)
        {
            /*
            List<Func<int>> funcs = new List<Func<int>>();

            for (int j = 0; j < 10; j++)
            {
                funcs.Add(() => j);
                Console.WriteLine(funcs[j]());
            }
            foreach (Func<int> func in funcs)
                Console.WriteLine(func());
            */
            /*測試delegate是否為不同Thread去執行:結論delegate不會開thread去跑*/
            List<Func<int>> funcs = new List<Func<int>>();
            for (int j = 0; j < 10; j++)
            {
                Console.WriteLine("For Thread Id:" + Thread.CurrentThread.ManagedThreadId);
                int tempJ = j;
                funcs.Add(() => { 
                    Console.WriteLine("Thread Id:" + Thread.CurrentThread.ManagedThreadId + ":" + tempJ); return tempJ; });
            }
            foreach (Func<int> func in funcs)
                Console.WriteLine(func.BeginInvoke(CallBack, func).AsyncState);
            new Thread(() =>
            {
                Console.WriteLine("New Thread Id:" + Thread.CurrentThread.ManagedThreadId);
            }).Start();
            /*
            for (int j = 0; j < 10; j++)
            {
                Func<int> func1 = () => j;
                Console.WriteLine(func1());
            }
            */
            Console.ReadKey();
        }


        static void CallBack(IAsyncResult ar)
        {
            ((Func<int>)ar.AsyncState).EndInvoke(ar);
            Console.WriteLine("CallBack Thread Id:" + Thread.CurrentThread.ManagedThreadId);
        }
    }
}
