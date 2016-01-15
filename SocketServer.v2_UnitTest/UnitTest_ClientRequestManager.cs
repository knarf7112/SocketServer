using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketServer.v2.Handlers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace SocketServer.v2_UnitTest
{
    [TestClass]
    public class UnitTest_ClientRequestManager
    {
        private ClientRequestManager manager;

        [TestInitialize]
        public void init()
        {
            this.manager = new ClientRequestManager();
        }

        [TestMethod]
        public void TestMethod_GetInstance()
        {
            
            int length = 500;
            IList<IClientRequestHandler> outputList = new List<IClientRequestHandler>();
            Task[] tasks = new Task[length];
            //get client form client manager by multithread, and it must be output one client object(maybe get old or create new)
            for (var i = 0; i < length; i++)
            {
                int tmp = i;
                //Trace.WriteLine("開始任務:" + tmp);
                var tmoObj = new { manager = this.manager, output = outputList };
                var t = Task.Factory.StartNew((obj) =>
                {
                    Trace.WriteLine("開始任務:" + tmp);
                    IClientRequestHandler client = null;
                    client = tmoObj.manager.GetInstance();
                    tmoObj.output.Add(client);
                }, tmoObj);
                tasks[i] = t;
            }
            Task.WaitAll(tasks);
            //check client list haven't null and client number is sequence
            for (int i = 0; i < length; i++)
            {
                Assert.IsNotNull(outputList[i]);
                Trace.WriteLine("outputList[" + i + "]:" + ((ClientRequestHandler)outputList[i]).ClientNo);
                Assert.IsTrue(((ClientRequestHandler)outputList[i]).ClientNo == (i + 1));
            }

            foreach (string clientstatus in this.manager.GetClientStatus())
            {
                Trace.WriteLine(clientstatus);
            }
        }

        [TestCleanup]
        public void Clean()
        {
            this.manager.ClearAll();
        }
    }
}
