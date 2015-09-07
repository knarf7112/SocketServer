using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
//
using Common.Logging;
using SocketServer;
//using System.Configuration;

namespace AuthenticateService
{
    public partial class AuthenticateService : ServiceBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AuthenticateService));

        private ISocketServer socketServer1;
        public AuthenticateService()
        {
            InitializeComponent();
            string[] authenticate = String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["AuthenticatePortAndServiceName"]) ? ("6111:Authenticate").Split(':') : System.Configuration.ConfigurationManager.AppSettings["AuthenticatePortAndServiceName"].Split(':');

            int authenticatePort = Int32.Parse(authenticate[0]);
            string authenticateServiceName = authenticate[1];
            this.socketServer1 = new AsyncMultiSocketServer(authenticatePort, authenticateServiceName);
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
