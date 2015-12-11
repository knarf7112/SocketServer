using System;
using System.ServiceProcess;
//
using Common.Logging;
using SocketServer;

namespace LoadKeyService
{
    public partial class LoadKeyService : ServiceBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LoadKeyService));

        private ISocketServer socketServer1;
        public LoadKeyService()
        {
            InitializeComponent();
            string[] loadKey = String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["LoadKeyPortAndServiceName"]) ? ("6112:LoadKey").Split(':') : System.Configuration.ConfigurationManager.AppSettings["LoadKeyPortAndServiceName"].Split(':');

            int loadKeyPort = Int32.Parse(loadKey[0]);
            string loadKeyServiceName = loadKey[1];
            this.socketServer1 = new AsyncMultiSocketServer(loadKeyPort, loadKeyServiceName);
        }

        protected override void OnStart(string[] args)
        {
            log.Debug(m => m("Start Service ..."));
            try
            {
                this.socketServer1.Start();
            }
            catch (Exception ex)
            {
                log.Error(m => m("[OnStart] Error:{0}", ex.ToString()));
            }
        }

        protected override void OnStop()
        {
            log.Debug(m => m("Stop Service ..."));
            this.socketServer1.Stop();
        }
    }
}
