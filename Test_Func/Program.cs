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
using System.Xml.Linq;
using System.Collections.Concurrent;
//
using Newtonsoft.Json;
//sqlite
using global::System.Data.SQLite;
using System.Data;
//regex
using System.Text.RegularExpressions;

namespace Test_Func
{
    class Program
    {
        #region 28.測試 Send Mail
        static bool mailSent = false;
        static void Main()
        {
            //ref:https://msdn.microsoft.com/zh-tw/library/system.net.mail.smtpclient(v=vs.110).aspx
            string smtpHost = "mail.allpay.com.tw";//用cmd 的nslookup查"192.168.50.2"對應的host Name;//SMTP host
            System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient(smtpHost);
            System.Net.Mail.MailAddress from = new System.Net.Mail.MailAddress("sys@allpay.com.tw","Server", Encoding.UTF8);
            System.Net.Mail.MailAddress to = new System.Net.Mail.MailAddress("knock.yang@allpay.com.tw", "Knock", Encoding.UTF8);
            System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage(from, to);
            msg.Body = "This is a test e-mail message sent by an application. 這是測試信件ㄟ";
            msg.BodyEncoding = Encoding.UTF8;

            msg.Subject = "TestKnockMail測試送信";
            msg.SubjectEncoding = Encoding.UTF8;

            //setting send mail complete
            client.SendCompleted += new System.Net.Mail.SendCompletedEventHandler((object obj, AsyncCompletedEventArgs e) => {
                // Get the unique identifier for this asynchronous operation.
                string token = (string)e.UserState;
                if (e.Cancelled)
                {
                    Console.WriteLine("[{0}] Send canceled.", token);
                }
                if (e.Error != null)
                {
                    Console.WriteLine("[{0}] {1}", token, e.Error.ToString());
                }
                else
                {
                    Console.WriteLine("Message sent.");
                }
                mailSent = true;
            });


            // The userState can be any object that allows your callback 
            // method to identify this send operation.
            // For this example, the userToken is a string constant.
            string userState = "test message1";
            client.SendAsync(msg, userState);
            Console.WriteLine("Sending message... press c to cancel mail. Press any other key to exit.");
            string answer = Console.ReadLine();
            // If the user canceled the send, and mail hasn't been sent yet,
            // then cancel the pending operation.
            if (answer.StartsWith("c") && mailSent == false)
            {
                client.SendAsyncCancel();
            }
            // Clean up.
            msg.Dispose();
            Console.WriteLine("Goodbye.");

        }
        #endregion

        #region 27.測試pipeline communication

        static void Main27()
        {
            //一開始以為可以跨Process互相交訊但不走網路,測試結果是只能同一個Process內交訊
            //ref:http://stackoverflow.com/questions/13806153/example-of-named-pipes
            StartServer();
            Task.Delay(1000).Wait();

            var client = new System.IO.Pipes.NamedPipeClientStream("PipesOfPiece");
            client.Connect();
            Console.WriteLine("client connected ");
            StreamReader sr = new StreamReader(client);
            StreamWriter sw = new StreamWriter(client);

            while (true)
            {
                Console.WriteLine("Console write any data");
                string line = Console.ReadLine();
                Console.WriteLine("Console write data:" + line);
                if (String.IsNullOrEmpty(line)) break;
                sw.WriteLine(line);
                Console.WriteLine("client write data :" + line);
                sw.Flush();
                string readData = sr.ReadLine();
                Console.WriteLine("client read data :" + readData);
            }
        }

        static void StartServer()
        {
            Task.Factory.StartNew(
                () => {
                    var server = new System.IO.Pipes.NamedPipeServerStream("PipesOfPiece");
                    server.WaitForConnection();

                    StreamReader sr = new StreamReader(server);
                    StreamWriter sw = new StreamWriter(server);
                    while (true)
                    {
                        
                        string line = sr.ReadLine();
                        Console.WriteLine("Server read data :" + line);
                        
                        sw.WriteLine(String.Join("", line.Reverse()));
                        Console.WriteLine("Server write data :" + String.Join("", line.Reverse()));
                        sw.Flush();
                    }
                });
        }
        #endregion

        #region 26.測試ToString("X")再PadLeft補0和直接ToString("X2")的差別,結果是一樣的
        static void Main26()
        {

            
            
            byte[] b1 = new byte[] { 1, 2, 3, 128, 255 };
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            for (int i = 0; i < b1.Length; i++)
            {

                sb.Append(b1[i].ToString("X").PadLeft(2, '0') + ",");//一樣的結果
                sb2.Append(b1[i].ToString("X2") + ",");//一樣的結果
            }
            Console.WriteLine(sb.ToString());
            Console.WriteLine(sb2.ToString());
                Console.ReadKey();
        }
        #endregion

        #region 25.Test Regex wrap \r\n

        static void Main25()
        {
            var qq = 0xFFFFFFFF;

            string newline = Environment.NewLine;
            string newline2 = "\r\n";
            byte[] newlineBytes = Encoding.ASCII.GetBytes(newline);
            byte[] newlineBytes2 = Encoding.ASCII.GetBytes(newline2);

            //websocket chrome send request sample 
            string hexRequest = "474554202F20485454502F312E310D0A486F73743A203132372E302E302E313A343534350D0A436F6E6E656374696F6E3A20557067726164650D0A507261676D613A206E6F2D63616368650D0A43616368652D436F6E74726F6C3A206E6F2D63616368650D0A557067726164653A20776562736F636B65740D0A4F726967696E" + 
                                "3A20687474703A2F2F737461636B6F766572666C6F772E636F6D0D0A5365632D576562536F636B65742D56657273696F6E3A2031330D0A557365722D4167656E743A204D6F7A696C6C612F352E30202857696E646F7773204E5420362E313B20574F57363429204170706C655765624B69742F3533372E333620284B48544D4C" +
                                "2C206C696B65204765636B6F29204368726F6D652F35302E302E323636312E3934205361666172692F3533372E33360D0A4163636570742D456E636F64696E673A20677A69702C206465666C6174652C20736463680D0A4163636570742D4C616E67756167653A207A682D54572C7A683B713D302E382C656E2D55533B713D30" +
                                "2E362C656E3B713D302E340D0A5365632D576562536F636B65742D4B65793A20456E54306F6D5A463272385A354F353372382B384D413D3D0D0A5365632D576562536F636B65742D457874656E73696F6E733A207065726D6573736167652D6465666C6174653B20636C69656E745F6D61785F77696E646F775F62697473";
            string request = Encoding.UTF8.GetString(HextoByte(hexRequest));
            string replacenewline = request.Replace("\r\n", "");
            //打算group所有的key TODO...
            //Regex reg = new Regex(@"(Connection: .*)?(Sec-WebSocket-Key: .*)?", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            string[] allParts = request.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            var request_key = allParts.Where(header => header.Contains("Sec-WebSocket-Key")).Select(key => key.Replace("Sec-WebSocket-Key:", "").Trim()).FirstOrDefault();
            Console.WriteLine("key:{0}", request_key);
            //Match match = reg.Match(request);
            //var ss = match.Groups[1].Value.Trim();
            //var ss = match.Groups[1].Value.Trim();
            Console.ReadKey();
        }

        static byte[] HextoByte(string hex)
        {
            if (hex.Length % 2 > 0)
                throw new ArgumentException("length not double");
            byte[] result = new byte[hex.Length >> 1];
            for (int i = 0; i < result.Length; i++)
            {
                //string hex2 = hex.Substring(j, 2);
                result[i] = Byte.Parse(hex.Substring(i<<1, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return result;
        }
        #endregion

        #region 24.Test byte[] combine
        static void Main24()
        {
            byte[] a = new byte[] { 1, 2, 3 };
            byte[] b = new byte[] { 4, 5, 6, 7 };
            byte[] c = new byte[] { 8, 9 };
            IEnumerable<byte> result = new byte[0];

            //call static method
            result = Enumerable.Concat(result, a);
            //
            result = result.Concat(a);//deffered combine
            result = result.Concat(b); 
            result = result.Concat(c);
            Console.WriteLine("byte[]:{0}", BitConverter.ToString(result.ToArray()));
            //foreach (byte b1 in result)
            //{
            //    Console.WriteLine("byte:{0}", b1);
            //}
            Console.ReadKey();
        }
        #endregion

        #region 23.測試非同步作業 IAsyncResult / AsyncCallback
        /// ref:https://dotblogs.com.tw/yc421206/2011/01/03/20540
        static void Main23(string[] args)
        {
            TestIAsyncResult t1 = new TestIAsyncResult();
            t1.CallAsyncMethod();
            Console.ReadKey();
        }
        #endregion

        #region 22.測試非同步是否消耗ThreadPool內的completionPortThreads(非同步IO)? 是:只要receive不結束那邊不結束,此非同步IO的thread就會在使用狀態(其他人無法使用)
        static void Main22(string[] args)
        {
            string leaveCmd = "exit";
            string data = "";
            //ref:
            IPEndPoint info = new IPEndPoint(IPAddress.Any,6111);
            ThreadPoolStatus("[Main]");
            Socket sck = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            sck.Bind(info);
            sck.Listen(1000);
            AsyncAccept(sck);
            ThreadPoolStatus("[Main]");
            do
            {
                data = Console.ReadLine();
            }
            while (data != leaveCmd);
            Console.WriteLine("88................");
            
        }
        static void AsyncAccept(Socket sck)
        {
            sck.BeginAccept(AcceptCallback, sck);
            ThreadPoolStatus("[AsyncAccept]");
        }
        static void AcceptCallback(IAsyncResult ar)
        {
            Socket main = ((Socket)ar.AsyncState);
            AsyncAccept(main);
            Socket client = main.EndAccept(ar);
            Console.WriteLine("產生了一個client...");
            string msg = "hello\r\n";
            client.Send(Encoding.ASCII.GetBytes(msg));
            ThreadPoolStatus("[AcceptCallback]送完數據");


            byte[] result = new byte[0];
            byte[] buffer;
            do{
                buffer = new byte[1024];
                int receiveLength = client.Receive(buffer);
                if(receiveLength != buffer.Length)
                Array.Resize(ref buffer,receiveLength);
                result = result.Concat(buffer).ToArray();
            }
            while(client.Available != 0);

            
            ThreadPoolStatus("[AcceptCallback]收完數據");


        }
        static void ThreadPoolStatus(string whocall)
        {
            //依據執行緒不同有不同數量 => 4個執行緒*250=1000個
            int workerThreads = 0;
            int completionPortThreads = 0;
            ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);//所以非同步IO是損耗completionPortThreads
            Console.WriteLine(whocall + ":可使用的thread總數:{0} 可使用的非同步thread總數:{1}", workerThreads, completionPortThreads);
        }
        #endregion

        #region 21.測試非同步client socket物件會不會造成thread遞增?結論不會
        static void Main21()
        {
            Console.WriteLine("push any key to create client ...");
            Console.ReadKey();
            string ip ="127.0.0.1";
            int port = 6111;
            int connect_Count = 10;
            ClientSocket client = new ClientSocket(ip, port);
            client.InitSocketClient(connect_Count);
            client.StartConnect_All();
            Console.WriteLine("End===========================================================================================");
            Console.ReadKey();

            Console.WriteLine("Start Close Connection========================================================================");
            client.CloseAll();
            Console.WriteLine("End===========================================================================================");
            Console.ReadKey();


            Console.WriteLine("Run 2 ===========================================================================================");
            client.InitSocketClient(connect_Count);
            client.StartConnect_All();
            Console.WriteLine("End2===========================================================================================");
            Console.ReadKey();
        }
        #endregion

        #region 20.測試與操作封裝SQLite的模組
        static void Main20()
        {
            
            string connectionstring = @"Data Source=database.db;";
            string tableName = "TestTable";
            string defined_paras = "(name varchar(20),age int,merch varchar(32),createTime varchar(14),EncZMK varchar(32))";
            IDbConnection conn = SQLiteFactory.Instance.CreateConnection();
            global::DB_Lib.Sqlite_Module sqlite_module = new DB_Lib.Sqlite_Module(connectionstring, conn);
            bool isSuccess = sqlite_module.Create_Table(tableName, defined_paras);
            
            //delete
            sqlite_module.Delete_Table(tableName);
        }
        #endregion

        #region 19.Process開啟其他執行檔並帶入參數,目前只確定可執行前附帶參數,已執行或執行中都無法在附帶參數
        static void Main19(string[] args)
        {
            //ref:https://msdn.microsoft.com/en-us/library/system.diagnostics.process.standardinput(v=vs.110).aspx
            Console.WriteLine("Please Input Command String");
            string cmd = Console.ReadLine();
            ProcessStartInfo psinfo = new ProcessStartInfo("cmd");//, "/c" + cmd);
            psinfo.RedirectStandardInput = true;
            psinfo.RedirectStandardOutput = true;
            psinfo.UseShellExecute = false;
            psinfo.CreateNoWindow = true;
            Process p = new Process();
            p.StartInfo = psinfo;
            p.Start();
            p.StandardInput.Write("dir/w");
            string result2 = p.StandardOutput.ReadToEnd();
            Console.WriteLine("Result:{0}", result2);
            /*
             * //無法在啟動程序後,再輸入參數帶入程序內,目前測試失敗,無法寫入,只能啟動前帶入參數
            string senddata = Console.ReadLine();
            p.StandardInput.WriteLine(senddata.TrimEnd('\n'));
            p.StandardInput.Flush();
            result2 = p.StandardOutput.ReadToEnd();
            Console.WriteLine("Result1:{0}", result2);
            */
            Console.ReadKey();
        }
        #endregion

        #region 18.測試HashSet用途:
        static void Main18(string[] args)
        {
            HashSet<string> hashList = new HashSet<string>();
            Console.WriteLine("knock hash code:{0}","knock".GetHashCode());
            hashList.Add("test123");
            hashList.Add("knock");
            hashList.Add("QQ123");
            hashList.Add("knock");//當加入時會去比較的HashCode是否相同,若有相同的就不再加入
            #region HashSet
            /*
            //測試HashSet 依據某個Hash集合來刪除共同的部分
            HashSet<string> hashList3 = new HashSet<string>();
            hashList3.Add("test123");
            hashList3.Add("knock");
            hashList.ExceptWith(hashList3);//刪除跟參數集合內擁有一樣的項目
             */
            HashSet<string> hashList3 = new HashSet<string>();
            hashList3.Add("test123");
            hashList3.Add("knock");
            hashList.UnionWith(hashList3);//

            hashList.Select((n,m) => {  Console.WriteLine(":{0}", n); return n; }).ToList();//tolist才會真正執行Iterator
            Console.ReadKey();
            #endregion
            foreach (var item in hashList)
            {
                Console.WriteLine("value:{0}", item);
            }
            Console.WriteLine("----------------------------");
            HashSet<Test> hashList2 = new HashSet<Test>();
            Test t1 = new Test(){ Id = 1, Name = "Test123", IsBool = true, data = new byte[]{10,20}};
            Test t2 = new Test(){ Id = 2, Name = "Test123", IsBool = true, data = new byte[]{10,20}};
            Test t3 = new Test(){ Id = 3, Name = "Test123", IsBool = true, data = new byte[]{10,20}};
            Test t4 = new Test(){ Id = 1, Name = "Test123", IsBool = true, data = new byte[]{10,20}};
            Test t5 = new Test(){ Id = 1, Name = "Test123", IsBool = true, data = new byte[]{10,20}};
            hashList2.Add(t1);
            hashList2.Add(t2);
            hashList2.Add(t3);
            hashList2.Add(t4);
            hashList2.Add(t5);
            Test t6 = t1;
            hashList2.Add(t6);//指向一樣的物件才會算是重複項目
            //結論:hashSet加入項目時會先比較項物的hash是否已存在列表中,若存在則不加入
            Console.ReadKey();
        }

        internal class Test
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool IsBool { get; set; }
            public byte[] data { get; set; }
            public Test Obj { get; set; }
        }
        #endregion

        #region 17.測試物件與繼承的子物件之間JSON轉換
        static void Main17(string[] args)
        {
            Stopwatch timer = new Stopwatch();
            //List<BaseA> qq = new List<InheritB>();//can't implicit...why? //TODO ...
            BaseA a = new BaseA { Id = 1, Name = "QQ" };
            InheritB b = new InheritB { Id = 2, Name = "WW", Address = "this is address", Checked = true };
            JsonSerializerSettings setting = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };
            #region List Base class A , class B 測試列舉物件轉JSON並測試有無implicit boxing的耗時比較
            IEnumerable<BaseA> listA = new List<BaseA>
            {
                new BaseA{ Id= 1, Name = "t1"},
                new BaseA{ Id= 2, Name = "t2"},
                new BaseA{ Id= 3, Name = "t3"}
            };
            string jsonListA = JsonConvert.SerializeObject(listA);
            Console.WriteLine("List Base A: {0}", jsonListA);
            IEnumerable<BaseA> listA2 = JsonConvert.DeserializeObject<IEnumerable<BaseA>>(jsonListA);

            IEnumerable<InheritB> listB = new List<InheritB>
            {
                new InheritB{ Id= 1, Name = "t1", Checked = true, Address = "this is t1 address"},
                new InheritB{ Id= 2, Name = "t2", Checked = false, Address = "this is t2 address"},
                new InheritB{ Id= 3, Name = "t3", Checked = true, Address = "this is t3 address"},
            };
            string jsonListB = JsonConvert.SerializeObject(listB);
            
            Console.WriteLine("List Base A: {0}", jsonListB);
            //just convert base class A properties
            IEnumerable<BaseA> listB2 = JsonConvert.DeserializeObject<IEnumerable<BaseA>>(jsonListB);
            timer.Start();
            //convert inherit class B properties but boxing to BaseA  : 測起來真的比較慢,因為又準備了一個類別來轉換且是放在heap,非stack
            //ref:https://msdn.microsoft.com/zh-tw/library/yz2be5wk.aspx
            IEnumerable<BaseA> listB3 = JsonConvert.DeserializeObject<IEnumerable<InheritB>>(jsonListB);
            timer.Stop();
            Console.WriteLine("Have Boxing TimeSpend:{0}", timer.ElapsedTicks);//値大約2xxx
            InheritB tt = listB3.First() as InheritB;
            //convert inherit class B properties
            timer.Restart();
            IEnumerable<InheritB> listB4 = JsonConvert.DeserializeObject<IEnumerable<InheritB>>(jsonListB);
            timer.Stop();
            Console.WriteLine("None Boxing TimeSpend:{0}", timer.ElapsedTicks);//値大約xx~1xx
            #endregion
            string jsonA = JsonConvert.SerializeObject(a);
            //string jsonA = JsonConvert.SerializeObject(a, setting);//setting加入會寫入物件type,即多一個$type屬性,値則是assembly Name
            string jsonB = JsonConvert.SerializeObject(b);
            Console.WriteLine("Base A: {0}", jsonA);
            Console.WriteLine("Inherit B: {0}", jsonB);
            BaseA a2 = JsonConvert.DeserializeObject<BaseA>(jsonA);//一般還原
            InheritB a3 = JsonConvert.DeserializeObject<InheritB>(jsonA);//將BaseA物件的JSON字串強制反序列化還原成InheritB也沒拋異常,但値會是null或是default
            BaseA a4 = JsonConvert.DeserializeObject<BaseA>(jsonB);//將InheritB的JSON字串反序列化還原成BaseA也沒問題,繼承的部分就不會轉換
            InheritB b2 = JsonConvert.DeserializeObject<InheritB>(jsonB);//看起來不加settings也可以反序列化還原回InheritB
            //Debugger.Break();
            Console.ReadKey();
        }

        [Serializable]
        internal class BaseA
        {
            public string Name { get; set; }
            public int Id { get; set; }
        }
        [Serializable]
        internal class InheritB : BaseA
        {
            public string Address { get; set; }
            public bool Checked { get; set; }
        }
        #endregion

        #region 16.測試async await 何時開分岔的? 結果:確定是從await之後才開岔產生另一個thread的
        //ref:http://huan-lin.blogspot.com/2016/01/async-and-await.html
        static void Main16(string[] args)
        {
            byte[] a = new byte[] { 10, 20, 30 };
            byte[] b = new byte[] { 1, 2, 3 };
            byte[] c = null;

            List<byte[]> list = new List<byte[]>();
            list.Add(c); list.Add(a); list.Add(b); list.Add(a); list.Add(b); list.Add(c); list.Add(b); list.Add(c); list.Add(a); list.Add(a);
            //var aa = list.Where(n => n != null).GroupBy(n => n);
            var aa = from i in list
                     group i by (list.Where( n=>n!=null)) into g1
                     select new { g = g1 };
            
            
            /**************************************************************/
            Console.WriteLine("1." + Thread.CurrentThread.ManagedThreadId);
            Task.Run(() =>
            {
                Console.WriteLine("3." + Thread.CurrentThread.ManagedThreadId);
                var result = get();
                result.ContinueWith(m => Console.WriteLine("4.5." + Thread.CurrentThread.ManagedThreadId + ":" + m.Result));
                Console.WriteLine("5." + Thread.CurrentThread.ManagedThreadId);
            });
            Console.WriteLine("2." + Thread.CurrentThread.ManagedThreadId);
            Console.ReadKey();
        }

        //static 

        static async Task<string> get()
        {
            Console.WriteLine("4:" + Thread.CurrentThread.ManagedThreadId);
            var qq = await rr();
            Console.WriteLine(qq + ":" + Thread.CurrentThread.ManagedThreadId);
            return qq;
        }

        static async Task<string> rr()
        {
            var ww = await Task.FromResult("test 123");
            Thread.Sleep(5000);
            return ww;
        }
        #endregion

        #region 15.讀取Xml設定檔=>Dictionary<List<來源IP>,List<目的IP>> ,輸入一組IP當來源並確認

        static void Main15(string[] args)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"\ProxyIPSettings.xml";
            IDictionary<IList<IPEndPoint>,IList<IPEndPoint>> dics = Load(path);
            //create a test endpoint
            //EndPoint ipTest = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 811);
            //EndPoint ipTest = new IPEndPoint(IPAddress.Parse("127.20.0.2"), 812);
            //search destination list from origin address at dictionary
            //IList<IPEndPoint> list = dics.FirstOrDefault(n => n.Key.Contains(ipTest)).Value;
            
            /********************************************************************************/
            //任意IP或任意Port的選擇測試(會選到IPEndPoint[ID:4,5,6,7])
            //EndPoint ipTest = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 999);//遠端資訊應為: (127.0.0.1:1200),(127.0.0.1:1210),(127.0.0.1:1220),(127.0.0.1:1230)
            //任意IP或任意Port的選擇測試(會選到IPEndPoint[ID:4,5,7]:任意IP指定Port)
            //EndPoint ipTest = new IPEndPoint(IPAddress.Parse("123.123.123.123"), 998);//遠端資訊應為: (127.0.0.1:1200),(127.0.0.1:1211),(           ),(127.0.0.1:1230)
            //任意IP或任意Port的選擇測試(會選到IPEndPoint[ID:4,6,7]:指定IP任意Port)
            EndPoint ipTest = new IPEndPoint(IPAddress.Parse("10.10.10.10"), 997);//遠端資訊應為: (127.0.0.1:1200),(              ),(127.0.0.1:1221),(127.0.0.1:1230)
            //任意IP或任意Port的選擇測試(會選到會選到IPEndPoint[ID:7]:指定IP任意Port)
            //EndPoint ipTest = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 65535);//遠端資訊應為: (             ),(             ),(            ),(127.0.0.1:1230)
            IEnumerable<IPEndPoint> mathesIpInfo =  GetDestinationList(dics, (IPEndPoint)ipTest);//這邊只是定義  ,還未取得結果
            mathesIpInfo = mathesIpInfo.ToList();//要執行過iterator才會進去跑
            foreach (var item in mathesIpInfo)
            {
                Console.WriteLine("符合設定檔的遠端資訊:{0}", item.ToString());
            }
            Console.WriteLine("End...");
            Console.ReadKey();
        }
        public static IDictionary<IList<IPEndPoint>,IList<IPEndPoint>> Load(string filePath)
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
                    IList<IPEndPoint> destinationlist = GetIpEndPointList(destinationNodes,"Destination");
                    //insert to dictionary
                    ipEndPointDic.Add(originlist, destinationlist);
                }
            }
            return ipEndPointDic;
        }

        protected static IList<IPEndPoint> GetIpEndPointList(IEnumerable<XElement> firstNodeGroups,string groupName)
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
                foreach (var item in childNodes)
                {
                    switch (item.Name.LocalName.ToUpper())
                    {
                        case "IP":
                            ip = (item.Value == "*") ? IPAddress.Any.ToString() : item.Value;
                            break;
                        case "PORT":
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

        #region 14.依據附檔名遞迴搜尋指定的目錄
        static void Main14(string[] args)
        {
            //依據附檔名搜尋此目錄下的所有符合的檔案
            var native = System.IO.Directory.GetFiles(@"D:\DL", "*.jpg", SearchOption.AllDirectories);
            int max = native.Length;
            int count = 0;
            string[] list = DirectorySearch(@"D:\DL", ".jpg");
            //以下是筆對是否兩邊一樣
            foreach (var item in native)
            {
                foreach (var my in list)
                {
                    if (item.Equals(my))
                    {
                        count += 1;
                        break;
                    }
                }
            }
            Console.WriteLine("使用原生的總數:{0}, 自己算完的總數:{1}, Match的總數:{2}", max, list.Length, count);
            Console.ReadKey();
        }
        //根據附檔名做目錄遞迴搜尋(PS:做爽的練習一下遞迴) 原生直接用Directory.GetFiles(@"D:\DL", "*.pdf", SearchOption.AllDirectories);
        public static string[] DirectorySearch(string rootDirectory, params string[] extentions)
        {
            string[] files = new string[0];
            string root = rootDirectory;
            string[] innerFiles = new string[0];
            string[] folderList = System.IO.Directory.GetDirectories(root, "*", System.IO.SearchOption.TopDirectoryOnly);
            //找當前目錄下的所有附檔名符合的檔案
            foreach (var extention in extentions)
            {
                files = System.IO.Directory.GetFiles(root, ("*" + extention), System.IO.SearchOption.TopDirectoryOnly);
            }
            //列舉出當前目錄下的所有第一層folder列表
            foreach (var dir in folderList)
            {
                //進入遞迴(DFS)
                innerFiles = DirectorySearch(dir, extentions);
                //跑完遞迴後把結果並加起來
                foreach (var extention in extentions)
                {
                    files = files.Concat(innerFiles).ToArray();
                }
            }
            return files;
        }
        #endregion

        #region 13.測試ProxySocket用的,要另外開2個nmap(一個當destination server,一個當origin client =>1.client要打proxy =>2.proxy轉給server =>3.server回傳 =>4.proxy回傳response)
        static void Main13(string[] args)
        {
            
            SocketProxy s2 = new SocketProxy(8112);
            s2.Start();
            Console.WriteLine("End...");
            Console.ReadKey();
            var ips = Dns.GetHostAddresses("127.0.0.1");
            string randing = (" ").PadLeft(12);
            Console.WriteLine(randing.Length);
        }
        #endregion

        #region 12.簡易偵測Windows系統中所有應用程式的啟動與結束
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

        #region 11.測試Parallel : 和一般for跑迴圈比較起來還比較慢,用途看來是為了能夠利用多核心,要開工作管理員來看了
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

        #region 10.測試Client Socket 的簡易發送訊息:有使用自己作的SocketClient.Domain.SocketClient的DLL(記得加入參考)
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

        #region 9.測試靜態成員是否只跑一次
        static void Main9()
        {
            TestClass1.AddStr();

            TestClass1 c1 = new TestClass1();//第一次使用會跑一次靜態成員
            //TestClass1.AddStr();
            TestClass1 c2 = new TestClass1();//看會不會再進去跑一次靜態成員,結論是不會再進去
            Console.ReadKey();
        }

        internal class TestClass1
        {
            public static IList<string> StrList = new List<string>();

            public static string Str = "test123";

            public static int Intarger = 999;

            public static IList<int> IntList = new List<int>();

            public static void AddStr()
            {
                var tmp = "a";
                for (var i = 1; i <= 10; i++)
                {

                    StrList.Add(tmp);
                    byte[] bytes = Encoding.ASCII.GetBytes(tmp);
                    bytes[0] += 1;// (byte)i;
                    tmp = Encoding.ASCII.GetString(bytes);
                }

            }
        }
        #endregion

        #region 8.比較timer並確認是否都是new thread出來執行的(確認current thread Id)
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

        #region 7.BeginXXX的方式測試AsyncMultiSocketServer.v2的執行檔,並轉各種格式字串轉成UriBuilder物件,方便取host和port和protocal名稱
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

        #region 6.測試Debugger物件與Enum變數用Reflection取値(TODO ...)
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
        
        #region 5.測試static共用於整個namespace下的那個類別
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

        #region 4.測試沒lock時的multi thread來增加數據到List內,假設同時新增數據會不會衝突,不過測試起來都沒出現異常,可能寫入數據太小
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

        #region 3.測試Async Await 使用方式
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

        #region 2.測試Task後的ContinueWith要做啥
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

        #region 1.測試delegate是否為不同Thread去執行:結論delegate不會開thread去跑
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
