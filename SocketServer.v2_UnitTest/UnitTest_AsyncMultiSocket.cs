using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
//
using System.Net;
using System.Net.Sockets;
//Test POCO
using SocketServer.v2;
//Log
using Common.Logging;
//
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace SocketServer.v2_UnitTest
{
    [TestClass]
    public class UnitTest_AsyncMultiSocket
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(UnitTest_AsyncMultiSocket));
        ISocketServer server1;
        ISocketServer server2;
        ISocketServer server3;
        CountdownEvent taskCount;
        [TestInitialize]
        public void Init()
        {
            this.server1 = new AsyncMultiSocketServer(8113,"一般加值和退貨");
            this.server2 = new AsyncMultiSocketServer(8114,"自動加值");
            this.server3 = new AsyncMultiSocketServer(8115, "自動加值查詢");
        }

        [TestMethod]
        public void TestMethod_AutoLoadQuery()
        {
            //測試自動加值的查詢功能(多開)
            //start service //要確定Back-End Service和KMS 2.0服務可用
            this.server3.Start();
            //send data(ComType:0531)
            string sendQueryhexData = "303130313031303533313030303030303031383630303030303030313836303430343242364139463239383020202020534554303031303031343434363331303031313030303030303039363030303030303936202020202020202020202020303250505050505050505050505050505050565656565656565656565656565656566817159989000028363831373135393938393030303032383230313630313031323032303132333132303136303131343137343735394134434337424346";
            byte[] sendQueryData = StringToByteArray(sendQueryhexData);//轉一下
            string url = "127.0.0.1";
            int port = 8115;
            int testCount = 20;//非同步連線總次數,太高就爆了
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(url), port);
            taskCount = new CountdownEvent(testCount);//callback sign告訴主程式完成任務用的
            string[] receiveList = new string[testCount];
            log.Debug("Client開始連線");
            for (int i = 0; i < testCount; i++)
            {
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                stateObj sendObj = new stateObj() { clientNo = i, mainSocket = client, sendData = sendQueryData, receiveSignList = taskCount, receiveDataList = receiveList };
                client.BeginConnect(ep, ConnectCallback, sendObj);
            }

            taskCount.Wait(13000);//等所有任務完成:最多等13秒
            //列出所有接收到的訊息
            for (int j = 0; j < receiveList.Length;j++)
            {
                log.Info("Client[" + j + "] data:" + receiveList[j]);
                Assert.IsNotNull(receiveList[j]);
            }
        }
        [TestMethod]
        public void TestMethod_AutoLoad()
        {
            //測試自動加值的功能(多開)
            //start service //要確定Back-End Service和KMS 2.0服務可用
            this.server2.Start();
            //send data(ComType:0332)
            string sendAutoLoadhexData = "3031303130313033333230303030303030313836303030303030303138363034303432423641394632393830202020205345543030313030313434343633313030313130303030303031343730303030303134372020202020202020202020203033202020202020202020202020202020205656565656565656565656565656565632303136303131333135353435393030303030313030303030303030303030303530303030303030303030303030303030303030303030681715998900002836383137313539393839303030303238323031363031303132303230303130313939393939393939414E4643314542353733";
            byte[] sendAutoLoadData = StringToByteArray(sendAutoLoadhexData);//轉一下
            string url = "127.0.0.1";
            int port = 8114;
            int testCount = 20;//非同步連線總次數,太高就爆了
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(url), port);
            taskCount = new CountdownEvent(testCount);//callback sign告訴主程式完成任務用的
            string[] receiveList = new string[testCount];
            log.Debug("Client開始連線");
            for (int i = 0; i < testCount; i++)
            {
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                stateObj sendObj = new stateObj() { clientNo = i, mainSocket = client, sendData = sendAutoLoadData, receiveSignList = taskCount, receiveDataList = receiveList };
                client.BeginConnect(ep, ConnectCallback, sendObj);
            }

            taskCount.Wait(13000);//等所有任務完成:最多等13秒
            //列出所有接收到的訊息
            for (int j = 0; j < receiveList.Length; j++)
            {
                log.Info("Client[" + j + "] data:" + receiveList[j]);
                Assert.IsNotNull(receiveList[j]);
            }
        }
        [TestMethod]
        public void TestMethod_AutoLoadTxLog()
        {
            //測試自動加值TxLog的功能(多開)
            //start service //要確定Back-End Service和KMS 2.0服務可用
            this.server2.Start();
            //send data(ComType:0332)
            string sendTxLoghexData = "3031303230313033333330303030303030313836303030303030303138363034333333434432453532303830202020204B444D3030313030313633303031313030313130303030303033353230303030303335322020202020202020202020203031303030383034323030373030303637353230313630313037313434313138303030303030303030383137313539393830303030313731303030303035303030343333334344324535323038303030303030313030303030303334303030303030313930303031323631303030303030303135303030313231313030343333334344324535323038303030303030303030303030303030303030303030303030313030303030353030303030303030303033313130393034444B444D30303130303136333030313130303131383630343333334344324535323038302020202030303030303530303630303731343038333438333030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030304132202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020";
            byte[] sendTxLogData = StringToByteArray(sendTxLoghexData);//轉一下
            string url = "127.0.0.1";
            int port = 8114;
            int testCount = 20;//非同步連線總次數,太高就爆了
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(url), port);
            taskCount = new CountdownEvent(testCount);//callback sign告訴主程式完成任務用的
            string[] receiveList = new string[testCount];
            log.Debug("Client開始連線");
            for (int i = 0; i < testCount; i++)
            {
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                stateObj sendObj = new stateObj() { clientNo = i, mainSocket = client, sendData = sendTxLogData, receiveSignList = taskCount, receiveDataList = receiveList };
                client.BeginConnect(ep, ConnectCallback, sendObj);
            }

            taskCount.Wait(13000);//等所有任務完成:最多等13秒
            //列出所有接收到的訊息
            for (int j = 0; j < receiveList.Length; j++)
            {
                log.Info("Client[" + j + "] data:" + receiveList[j]);
                Assert.IsNotNull(receiveList[j]);
            }
        }

        [TestMethod]
        public void TestMethod_Load()
        {
            //測試一般加值的功能(多開)
            //start service //要確定Back-End Service和KMS 2.0服務可用
            this.server1.Start();
            //send data(ComType:0331)
            string sendLoadhexData = "30313031303130333331303030303030303138363030303030303031383630343046334344324535323038302020202053455430303130303131313131393130303131303030303030313435303030303031343520202020202020202020202030312020202020202020202020202020202031352E37342E31302020202020202020323031363031303631343539353530303030313730303030303030303030303032303030303030000000000000000000000000000000007216120665000017373231363132303636353030303031373030303030303030323031373132333130303030303132393332453744353835";
            byte[] sendLoadData = StringToByteArray(sendLoadhexData);//轉一下
            string url = "127.0.0.1";
            int port = 8113;
            int testCount = 10;//非同步連線總次數,太高就爆了
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(url), port);
            taskCount = new CountdownEvent(testCount);//callback sign告訴主程式完成任務用的
            string[] receiveList = new string[testCount];
            log.Debug("Client開始連線");
            for (int i = 0; i < testCount; i++)
            {
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                stateObj sendObj = new stateObj() { clientNo = i, mainSocket = client, sendData = sendLoadData, receiveSignList = taskCount, receiveDataList = receiveList };
                //Thread.Sleep(100);
                client.BeginConnect(ep, ConnectCallback, sendObj);
            }

            taskCount.Wait(18000);//等所有任務完成:最多等18秒
            //列出所有接收到的訊息
            for (int j = 0; j < receiveList.Length; j++)
            {
                
                log.Info("Client[" + j + "] data:" + receiveList[j]);
                Assert.IsNotNull(receiveList[j]);
            }
        }

        [TestMethod]
        public void TestMethod_LoadTxLog()
        {
            //測試一般加值TxLog的功能(多開)
            //start service //要確定Back-End Service和KMS 2.0服務可用
            this.server1.Start();
            //send data(ComType:0341)
            string sendLoadTxLoghexData = "30313032303130333431303030303030303138363030303030303031383630343341314436323946323938302020202053455430303130303937353035343130303231303030303030333532303030303033353220202020202020202020202030313331313037303636383134343239353232303135313132363233353934363030303030303030373331313135303634333030333435333030303030323030303433413144363239463239383030303133303330303030303031323030303030303035303030303231303030303030303030373030303031373039303433413144363239463239383030303030303030303030303030303030303030303032313830303133343132353030303030303030424339323241323953455430303130303937353035343130303231383630343341314436323946323938302020202030303030303339313030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030304132202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020";
            byte[] sendLoadTxLogData = StringToByteArray(sendLoadTxLoghexData);//轉一下
            string url = "127.0.0.1";
            int port = 8113;
            int testCount = 10;//非同步連線總次數,太高就爆了
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(url), port);
            taskCount = new CountdownEvent(testCount);//callback sign告訴主程式完成任務用的
            string[] receiveList = new string[testCount];
            log.Debug("Client開始連線");
            for (int i = 0; i < testCount; i++)
            {
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                stateObj sendObj = new stateObj() { clientNo = i, mainSocket = client, sendData = sendLoadTxLogData, receiveSignList = taskCount, receiveDataList = receiveList };
                client.BeginConnect(ep, ConnectCallback, sendObj);
            }

            taskCount.Wait(18000);//等所有任務完成:最多等18秒
            //列出所有接收到的訊息
            for (int j = 0; j < receiveList.Length; j++)
            {
                log.Info("Client[" + j + "] data:" + receiveList[j]);
                Assert.IsNotNull(receiveList[j]);
            }
        }

        [TestMethod]
        public void TestMethod_PurchaseReturn()
        {
            //測試一般加值TxLog的功能(多開)
            //start service //要確定Back-End Service和KMS 2.0服務可用
            this.server1.Start();
            //send data(ComType:0631)
            string sendPRhexData = "30313031303130363331303030303030303138363030303030303031383630343430323736413946323938302020202053455430303130303935393132323130303131303030303030313435303030303031343520202020202020202020202030322020202020202020202020202020202031352E37322E30312020202020202020323031353038313130303033333630353534393330303030303030303030303030343030303030000000000000000000000000000000007311140148024009373331313134303134383032343030393030303030303030323031373038313130303030303034393641383031393544";
            byte[] sendPRData = StringToByteArray(sendPRhexData);//轉一下
            string url = "127.0.0.1";
            int port = 8113;
            int testCount = 20;//非同步連線總次數,太高就爆了
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(url), port);
            taskCount = new CountdownEvent(testCount);//callback sign告訴主程式完成任務用的
            string[] receiveList = new string[testCount];
            log.Debug("Client開始連線");
            for (int i = 0; i < testCount; i++)
            {
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                stateObj sendObj = new stateObj() { clientNo = i, mainSocket = client, sendData = sendPRData, receiveSignList = taskCount, receiveDataList = receiveList };
                client.BeginConnect(ep, ConnectCallback, sendObj);
            }

            taskCount.Wait(18000);//等所有任務完成:最多等18秒
            //列出所有接收到的訊息
            for (int j = 0; j < receiveList.Length; j++)
            {
                log.Info("Client[" + j + "] data:" + receiveList[j]);
                Assert.IsNotNull(receiveList[j]);
            }
        }

        [TestMethod]
        public void TestMethod_PurchaseReturnTxLog()
        {
            //測試一般加值TxLog的功能(多開)
            //start service //要確定Back-End Service和KMS 2.0服務可用
            this.server1.Start();
            //send data(ComType:0641)
            string sendPRhexData = "30313032303130363431303030303030303138363030303030303031383630343237323036323946323938302020202053455430303130303134383730383130303131303030303030333532303030303033353220202020202020202020202030313138373835303032303031353935353332303136303130353130313230333030303030303030373231313133303938333030303034393030303030313331303432373230363239463239383030303032353530303030303534393030303030313732303030393534373530303030303337373030303934353331303432373230363239463239383030303030303030303030303030303030303030303030343930303031373937343030313331303030303030303030303030303030344342363231364553455430303130303134383730383130303131383630343237323036323946323938302020202030303030303934343030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030304132202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020";
            byte[] sendPRData = StringToByteArray(sendPRhexData);//轉一下
            string url = "127.0.0.1";
            int port = 8113;
            int testCount = 20;//非同步連線總次數,太高就爆了
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(url), port);
            taskCount = new CountdownEvent(testCount);//callback sign告訴主程式完成任務用的
            string[] receiveList = new string[testCount];
            log.Debug("Client開始連線");
            for (int i = 0; i < testCount; i++)
            {
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                stateObj sendObj = new stateObj() { clientNo = i, mainSocket = client, sendData = sendPRData, receiveSignList = taskCount, receiveDataList = receiveList };
                client.BeginConnect(ep, ConnectCallback, sendObj);
            }

            taskCount.Wait(13000);//等所有任務完成:最多等13秒
            //列出所有接收到的訊息
            for (int j = 0; j < receiveList.Length; j++)
            {
                log.Info("Client[" + j + "] data:" + receiveList[j]);
                Assert.IsNotNull(receiveList[j]);
            }
        }

        //end Connect用的callabck方法
        private void ConnectCallback(IAsyncResult ar)
        {
            if (ar.IsCompleted)
            {
                stateObj obj = null;
                try
                {
                    obj = (stateObj)ar.AsyncState;
                    obj.mainSocket.EndConnect(ar);
                    //Thread.Sleep(obj.clientNo * 10);//加入延遲,結果一樣後端延遲
                    int sendLength = obj.mainSocket.Send(obj.sendData);
                    byte[] buffer = new byte[0x1000];
                    int receivelength = obj.mainSocket.Receive(buffer);
                    Array.Resize(ref buffer, receivelength);
                    obj.receiveDataList[obj.clientNo] = BitConverter.ToString(buffer).Replace("-", "");

                }
                catch (Exception ex)
                {
                    if (obj != null)
                    {
                        log.Error("Client[" + obj.clientNo +"]連線異常:" + ex.Message);
                    }
                }
                finally
                {
                    if (obj != null)
                    {
                        if (obj.mainSocket != null)
                        {
                            try
                            {
                                obj.mainSocket.Shutdown(SocketShutdown.Both);
                            }
                            catch (Exception ex)
                            {
                                log.Error("client[" + obj.clientNo + "]關閉時爆了" + ex.Message);
                            }
                            obj.mainSocket.Close(1000);
                            obj.mainSocket.Dispose();
                            obj.mainSocket = null;
                        }
                        //mission complete
                        obj.receiveSignList.Signal();
                        //remove point
                        obj.receiveSignList = null;
                        obj.receiveDataList = null;
                    }
                }
            }
        }
        //hex轉byte用的
        public static byte[] StringToByteArray(String hex)
        {
            hex = hex.TrimEnd('\r', '\n');//trim CRLF
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
        //state object
        class stateObj
        {
            public int clientNo;//client編號
            public Socket mainSocket;//主socket client
            public byte[] sendData;//送出資料
            public CountdownEvent receiveSignList;//完成任務時設定sign告訴主線完成任務
            public string[] receiveDataList;//依據client編號,依序設定接收資料data[0]=>client 1 接收的hex string
        }
        [TestCleanup]
        public void Clear()
        {
            this.server1.Stop();
            this.server2.Stop();
            this.server3.Stop();
        }
    }
}
