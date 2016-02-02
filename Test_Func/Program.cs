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
using System.Timers;
using Test_Func.Test;
using System.IO;
// need using =>Main12
using System.Management;

namespace Test_Func
{
    class Program
    {
        static void Main(string[] args)
        {
            SocketProxy s2 = new SocketProxy();
            s2.AcceptConnect();
            Console.WriteLine("End...");
            Console.ReadKey();
            var ips = Dns.GetHostAddresses("127.0.0.1");
            string randing = (" ").PadLeft(12);
            Console.WriteLine(randing.Length);
        }

        #region 簡易偵測Windows系統中所有應用程式的啟動與結束
        //想偵測Windows系統中所有應用程式的啟動與結束
        //ref:https://dotblogs.com.tw/code6421/2015/06/02/151461
        static void Main12()
        {

            //要using wmi的dll=>System.Management
            ManagementEventWatcher wmiProcessWatcher = null, wmiProcessEndingWatcher = null ;
            Task.Run(() =>
            {
                try
                {
                    //建立監視物件並設定Query字串
                    wmiProcessWatcher = new ManagementEventWatcher("SELECT *FROM __InstanceCreationEvent WITHIN 0.1 WHERE TargetInstance ISA 'Win32_Process'");
                    wmiProcessEndingWatcher = new ManagementEventWatcher("SELECT * FROM __InstanceDeletionEvent WITHIN 0.1 WHERE TargetInstance ISA 'Win32_Process'");
                    wmiProcessWatcher.EventArrived += (object sender, EventArrivedEventArgs e) =>
                    {
                        string str = (string)((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value).Properties["Name"].Value;
                        Console.WriteLine(string.Format("process {0} created", str));
                    };
                    wmiProcessEndingWatcher.EventArrived += new EventArrivedEventHandler((object sender, EventArrivedEventArgs e) => {
                        string str = (string)((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value).Properties["Name"].Value;
                        Console.WriteLine(string.Format("process {0} terminated", str));
                    });
                    wmiProcessWatcher.Start();
                    wmiProcessEndingWatcher.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
            Console.WriteLine("press any key to exit");
            if (wmiProcessWatcher != null)
                wmiProcessWatcher.Stop();
            if (wmiProcessEndingWatcher != null)
                wmiProcessEndingWatcher.Stop();
            Console.ReadKey();
        }
        #endregion

        #region 測試Parallel : 和一般for跑迴圈比較起來還比較慢,用途看來是為了能夠利用多核心,要開工作管理員來看了
        static void Main11(string[] args)
        {
            Console.WriteLine("Start Parallel");
            //用黑大的測試看來比較準 http://blog.darkthread.net/blogs/darkthreadtw/archive/2010/01/03/multicore-2.aspx
            int MAX_COUNT = 5000 * 10000;
            Stopwatch timer = new Stopwatch();
            for (int round = 0; round < 10; round++)
            {
                timer.Restart();
                for (int i = 0; i < MAX_COUNT; i++)
                {
                    double d = Math.Log10(Convert.ToDouble(i));
                }
                timer.Stop();
                Console.WriteLine("循序處理 = {0:N0}ms", timer.ElapsedMilliseconds);
                timer.Restart();

                int WORK_COUNT = 4;
                Thread[] workers = new Thread[WORK_COUNT];
                int jobCountPerWork = MAX_COUNT / WORK_COUNT;
                for (int i = 0; i < WORK_COUNT; i++)
                {
                    //將全部工作切成WORKER_COUNT份，
                    //分給WORKER_COUNT個Thread執行
                    int st = jobCountPerWork * i;
                    int ed = jobCountPerWork * (i + 1);
                    if (ed > MAX_COUNT)
                        ed = MAX_COUNT;
                    workers[i] = new Thread(() =>
                    {
                        //Console.WriteLine("LOOP: {0:N0} - {1:N0}", st, ed);
                        for (int j = st; j < ed; j++)
                        {
                            double d = Math.Log10(Convert.ToDouble(j));
                        }
                    });
                    workers[i].Start();
                }
                for (int i = 0; i < WORK_COUNT; i++)
                    workers[i].Join();
                timer.Stop();
                Console.WriteLine("平行處理[{1}] = {0:N0}ms", timer.ElapsedMilliseconds, WORK_COUNT);
                timer.Restart();
                Parallel.For(0, MAX_COUNT, (int j) =>
                {
                    double d = Math.Log10(Convert.ToDouble(j));
                });
                timer.Stop();
                Console.WriteLine("平行處理2 = {0:N0}ms", timer.ElapsedMilliseconds);
            }
            /****************************************************************************/
            //int length = Int32.MaxValue;
            //ref:http://blog.lyhdev.com/2010/12/c-task-parallel-library.html
            //循序處理的寫法:I7 4核心測試發現CPU消耗根本只有1%
            //for (int i = 0; i < length / 2; i++)
            //{
            //    Math.Sqrt(i);
            //    Console.WriteLine(Math.Sqrt(i));
            //}
            //平行處理的寫法:I7 4核心測試發現8個執行緒都可以達到100%, 程序確實消耗80~90%
            //Parallel.For(0, length / 2, (i) =>
            //{
            //    Console.WriteLine(Math.Sqrt(i));
            //});
            /*****************************************************************************************/
            //string testStr = "heLLo, worLd";
            //Stopwatch timer = new Stopwatch();
            //Console.WriteLine("Start Parallel Function ...");
            //timer.Start();
            ////c:每個char , state:ParallelLoopState Object , i:index
            //Parallel.ForEach(testStr, (c, state, i) =>
            //{
            //    if (c.Equals('w')) { state.Stop(); }
            //    Console.WriteLine(i + ":" + c.ToString() + ":" + state.IsStopped);
            //});
            //timer.Stop();
            //Console.WriteLine("Parallel TimeSpend: {0}ms", timer.ElapsedMilliseconds);
            //timer.Restart();
            //for (int i = 0; i < testStr.Length; i++)
            //{
            //    Console.WriteLine(i + ":" + testStr[i].ToString());
            //}
            //timer.Stop();
            //Console.WriteLine("For TimeSpend: {0}ms", timer.ElapsedMilliseconds);
            //Console.WriteLine("End Test ...");
            Console.ReadKey();
        }
        #endregion

        #region 測試Client Socket 的簡易發送訊息:有使用自己作的SocketClient.Domain.SocketClient的DLL(記得加入參考)
        static void Main10(string[] args)
        {
            for(var i = 0; i < args.Length;i++)
            {
                Console.WriteLine("輸入參數[{0}]:{1}", i, args[i]);//給CMD跑的時候用的,應該沒用到(測試)
            }
            //var qq = Convert.ToByte("22", 16);
            Console.WriteLine("輸入目的IP => ex: xxx.xx.xx.x");
            string ip = Console.ReadLine();//input data
            Console.WriteLine("輸入目的Port => ex: 6101");
            int port = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("請輸入hex string");
            string data = ReadLine();//Console.ReadLine();//因為原來的Console只能讀取到256個char,所以取出資料流改緩衝大小再讀

            Console.WriteLine("send data(hex string length:{0}):{1}",data.Length,data);//除2就是byte length
            byte[] sendData = StringToByteArray(data);
            byte[] receiveData = null;
            try
            {
                using (SocketClient.Domain.SocketClient client = new SocketClient.Domain.SocketClient(ip, port))
                {
                    if (client.ConnectToServer())
                    {
                        receiveData = client.SendAndReceive(sendData);
                        Console.WriteLine("Receive Data(byte length:{0}): {1}", receiveData.Length, BitConverter.ToString(receiveData).Replace("-", ""));
                    }
                }
            }
            catch (SocketException sckEx)
            {
                Console.WriteLine("Socket 異常:{0}", sckEx.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("一般異常:{0}", ex.Message);
            }
            Console.WriteLine("結束 ...");
            Console.ReadKey();
        }
        //ref:http://stackoverflow.com/questions/5557889/console-readline-max-length
        public static string ReadLine()
        {
            int size = 0x1000;
            //將Console的
            Stream inputStream = Console.OpenStandardInput(size);//取出Console的標準輸入流並指定緩衝大小
            byte[] buffer = new byte[size];
            int outputLength = inputStream.Read(buffer, 0, size);//使用Stream來讀取資料到緩衝buffer變數
            char[] chars = Encoding.UTF7.GetChars(buffer, 0, outputLength);//使用UTF7轉換buffer內的資料,轉成char陣列
            return new String(chars);
        }
        public static byte[] StringToByteArray(String hex)
        {
            //http://stackoverflow.com/questions/1038031/what-is-the-easiest-way-in-c-sharp-to-trim-a-newline-off-of-a-string
            hex = hex.TrimEnd('\r', '\n');//trim CRLF
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
        #endregion

        #region 測試靜態成員是否只跑一次
        static void Main9()
        {
            TestClass1.AddStr();

            TestClass1 c1 = new TestClass1();//第一次使用會跑一次靜態成員
            //TestClass1.AddStr();
            TestClass1 c2 = new TestClass1();//看會不會再進去跑一次靜態成員
            Console.ReadKey();
        }
        #endregion

        #region 比較timer並確認是否都是new thread出來執行的(確認current thread Id)
        static void Main8()
        {
            Console.WriteLine("開始跑太碼...thread Id:{0}", Thread.CurrentThread.ManagedThreadId);
            //timer 1
            System.Timers.Timer t1 = new System.Timers.Timer(1000);
            t1.Elapsed += t1_Elapsed;
            t1.Start();
            //timer 2
            string state = "this is threading Timer";
            //System.Threading.Timer t2 = new System.Threading.Timer(new TimerCallback(callback), state, 1000, 1000);
            //結論:確認兩種timer都是產生一個或多個thread去跑
            Console.ReadKey();
        }

        static void t1_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("[Timer 1]目前Thread ID:{0} \t {1}", Thread.CurrentThread.ManagedThreadId,e.SignalTime.ToString("HH:mm:ss"));
        }

        static void callback(object obj)
        {
            Console.WriteLine("[Timer 2]目前Thread ID:{0} \t {1}", Thread.CurrentThread.ManagedThreadId, (string)obj);
        }
        #endregion

        #region BeginXXX的方式測試AsyncMultiSocketServer.v2的執行檔,並轉各種格式字串轉成UriBuilder物件,方便取host和port和protocal名稱
        static void Main7()
        {
            //測試uri轉換格式
            //ref:http://stackoverflow.com/questions/20164298/how-to-build-a-url
            string url1 = "qqq:127.0.0.1:8081";
            string url2 = "http://www.gg.com.tw:9601/hahaha/test123?id=999&age=888";
            UriBuilder uri = new UriBuilder(url1);
            
            //Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"),6112);
            Console.WriteLine("開始測試連線");
            for(var i = 0; i < 800; i++){
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                stateObj clientState = new stateObj() { mainsck = client, send = "012340331" + i.ToString("D4") };
                client.BeginConnect(ep, ConnectCallBack, clientState);
            }
            
            Console.ReadKey();
        }
        static void ConnectCallBack(IAsyncResult ar)
        {
            if (ar.AsyncWaitHandle.WaitOne(1000) && ar.IsCompleted)
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
        #endregion

        #region 測試Debugger物件與Enum變數用Reflection取値(TODO ...)
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
        #endregion
        
        #region 測試static共用於整個namespace下的那個類別
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
        #endregion

        #region 測試沒lock時的multi thread來增加數據到List內,假設同時新增數據會不會衝突,不過測試起來都沒出現異常,可能寫入數據太小
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
        #endregion

        #region 測試Async Await 使用方式
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
        #endregion

        #region 測試Task後的ContinueWith要做啥
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
                //Task.Factory.StartNew<qq>((object obj, qq t) =>
                //{
                //    Thread.Sleep(3000 - t.i * 100);
                //    Console.WriteLine("Asynchronization M1:" + t.i + ": sleep:" + t.r);
                //    t.i = t.i + 1;
                //    return t;
                //}, ttt);
                p.M1(ttt).ContinueWith(iii =>
                {
                    Console.WriteLine("continue:" + ((qq)iii.Result).i + ": sleep:" + ((qq)iii.Result).r);
                });
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
        #endregion

        #region 測試delegate是否為不同Thread去執行:結論delegate不會開thread去跑
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
        #endregion
    }
}
