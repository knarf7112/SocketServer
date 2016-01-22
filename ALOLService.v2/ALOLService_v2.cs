using System.ServiceProcess;
//Log
using Common.Logging;
//
using SocketServer.v2;
using System;
namespace ALOLService.v2
{
    public partial class ALOLService_v2 : ServiceBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ALOLService_v2));
        ISocketServer socketServer1;
        ISocketServer socketServer2;
        ISocketServer socketServer3;
        public ALOLService_v2()
        {
            InitializeComponent();
            string autoload = String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["AutoLoadPortAndServiceName"]) ? "8114:AutoLoad" : System.Configuration.ConfigurationManager.AppSettings["AutoLoadPortAndServiceName"];
            string autoloadPort = autoload.Split(':')[0];
            string autoloadServiceName = autoload.Split(':')[1];
            socketServer1 = new AsyncMultiSocketServer(Convert.ToInt32(autoloadPort), autoloadServiceName);
            string autoloadQuery = String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["AutoLoadQueryPortAndServiceName"]) ? "8115:AutoLoadQuery" : System.Configuration.ConfigurationManager.AppSettings["AutoLoadQueryPortAndServiceName"];
            string autoloadQueryPort = autoloadQuery.Split(':')[0];
            string autoloadQueryServiceName = autoloadQuery.Split(':')[1];
            socketServer2 = new AsyncMultiSocketServer(Convert.ToInt32(autoloadQueryPort), autoloadQueryServiceName);
            string loadAndPurchaseReturn = String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["LoadAndPurchaseReturnPortAndServiceName"]) ? "8113:LoadAndPurchaseReturn" : System.Configuration.ConfigurationManager.AppSettings["LoadAndPurchaseReturnPortAndServiceName"];
            string loadAndPurchaseReturnPort = loadAndPurchaseReturn.Split(':')[0];
            string loadAndPurchaseReturnServiceName = loadAndPurchaseReturn.Split(':')[1];
            socketServer3 = new AsyncMultiSocketServer(Convert.ToInt32(loadAndPurchaseReturnPort), loadAndPurchaseReturnServiceName);
        }

        protected override void OnStart(string[] args)
        {
            log.Debug(m=>m("Start Service..."));
            try
            {
                this.socketServer1.Start();
                // none-block, continue....
                this.socketServer2.Start();
                // none-block, continue....
                this.socketServer3.Start();
                // none-block, continue....
            }
            catch (Exception ex)
            {
                log.Error(m => m("啟動服務異常: {0} \r\n{1}", ex.Message, ex.StackTrace));
            }
        }

        protected override void OnStop()
        {
            log.Debug(m=>m("Stop Service..."));
            try
            {
                this.socketServer1.Stop();
                // none-block, continue....
                this.socketServer2.Stop();
                // none-block, continue....
                this.socketServer3.Stop();
                // none-block, continue....
            }
            catch (Exception ex)
            {
                log.Error(m => m("關閉服務異常: {0} \r\n{1}", ex.Message, ex.StackTrace));
            }
        }
    }
}
