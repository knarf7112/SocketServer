using System;
using System.ServiceProcess;
//
using Common.Logging;
//
using SocketServer.v2;

namespace PAMService
{
    public partial class PAMQuotaService : ServiceBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PAMQuotaService));
        ISocketServer socketServer1;
        public PAMQuotaService()
        {
            InitializeComponent();
            string pamQuota = String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["PAMQuotaPortAndServiceName"]) ? "8116:PAMQuota" : System.Configuration.ConfigurationManager.AppSettings["PAMQuotaPortAndServiceName"];
            string pamQuotaPort = pamQuota.Split(':')[0];
            string pamQuotaServiceName = pamQuota.Split(':')[1];
            socketServer1 = new AsyncMultiSocketServer(Convert.ToInt32(pamQuotaPort), pamQuotaServiceName);
        }

        protected override void OnStart(string[] args)
        {
            log.Debug(m=>m("Start Window Service..."));
            try
            {
                this.socketServer1.Start();
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
            }
            catch (Exception ex)
            {
                log.Error(m => m("關閉服務異常: {0} \r\n{1}", ex.Message, ex.StackTrace));
            }
        }
    }
}
