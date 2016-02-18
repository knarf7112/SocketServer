using System;
using System.Threading;
//
using Common.Logging;
//
using Kms2.Crypto;
using Kms2.Crypto.Common;
using Kms2.Crypto.Utility;
//Spring Container
using Spring.Context.Support;
using Spring.Context;
//

namespace SocketServer.v2
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        //測試Dennis的dll(Spring.net)
        static void Main1(string[] args)
        {
            //app.config要先加入設定的object,目前已在Assembly file內loading,要改成可變更需拿出來變resource file
            IApplicationContext ctx = ContextRegistry.GetContext();
            IHexConverter hexConverter = ctx["hexConverter"] as IHexConverter;
            IByteWorker byteWorker = ctx["byteWorker"] as IByteWorker;
            IMac2Manager mac2Manager = ctx["mac2Manager"] as IMac2Manager;
            IHashWorker hashWorker = ctx["hashWorker"] as IHashWorker;
            IMsgUtility msgUtility = ctx["icash2AOLReqMsgUtility"] as IMsgUtility;
            IMsgUtility PAMReqMsgUtility = ctx["icash2PAMReqMsgUtility"] as IMsgUtility;
            IMsgUtility PAMRespMsgUtility = ctx["icash2PAMRespMsgUtility"] as IMsgUtility;
            Console.ReadKey();
        }
        /**************************************************************************************/
        static void Main(string[] args)
        {
            /*
            log.Debug("Run Log:" + DateTime.Now.ToString("HH:mm:ss.fff") );//測試結果:log4net 寫入時間與真實當下執行的時間誤差約10~30ms
            Console.WriteLine(ConfigLoader.GetSettings(ConType.AutoLoad));
            //Console.ReadKey();
            ClientRequestManager manager = new ClientRequestManager();
            IClientRequestHandler client = manager.GetInstance();
            Console.ReadKey();
            */
            //AsyncMultiSocketServer s1 = new AsyncMultiSocketServer(8115, "AutoLoad");
            AsyncMultiSocketServer s1 = new AsyncMultiSocketServer(8116, "PAMQuota");
            Console.WriteLine("Main Thread Id:{0}  background:{1}", Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.IsBackground.ToString());
            s1.Start();
            //
            Console.WriteLine("服務開始");
            Console.ReadKey();
            s1.Stop();
            Console.ReadKey();
        }
    }
}
