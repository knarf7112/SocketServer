﻿using System;
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
        private ISocketServer socketServer2;
        //private ISocketServer socketServer3;
        public LoadKeyService()
        {
            InitializeComponent();
            string[] loadKey = String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["LoadKeyPortAndServiceName"]) ? ("6112:LoadKey").Split(':') : System.Configuration.ConfigurationManager.AppSettings["LoadKeyPortAndServiceName"].Split(':');

            int loadKeyPort = Int32.Parse(loadKey[0]);
            string loadKeyServiceName = loadKey[1];
            this.socketServer1 = new AsyncMultiSocketServer(loadKeyPort, loadKeyServiceName);

            string[] loadKeyTxLog = String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["LoadKeyTxLogPortAndServiceName"]) ? ("6113:LoadKeyTxLog").Split(':') : System.Configuration.ConfigurationManager.AppSettings["LoadKeyTxLogPortAndServiceName"].Split(':');

            int loadKeyTxLogPort = Int32.Parse(loadKeyTxLog[0]);
            string loadKeyTxLogServiceName = loadKeyTxLog[1];
            this.socketServer2 = new AsyncMultiSocketServer(loadKeyTxLogPort, loadKeyTxLogServiceName);

            //string[] loadKeyList = String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["LoadKeyListPortAndServiceName"]) ? ("6114:LoadKeyList").Split(':') : System.Configuration.ConfigurationManager.AppSettings["LoadKeyListPortAndServiceName"].Split(':');
            //int loadKeyListPort = Int32.Parse(loadKeyList[0]);
            //string loadKeyListServiceName = loadKeyList[1];
            //this.socketServer3 = new AsyncMultiSocketServer(loadKeyListPort, loadKeyListServiceName);
        }

        protected override void OnStart(string[] args)
        {
            log.Debug(m => m("OnStart Service ..."));
            try
            {
                this.socketServer1.Start();
                this.socketServer2.Start();
                //this.socketServer3.Start();
            }
            catch (Exception ex)
            {
                log.Error(m => m("[OnStart] Error:{0}", ex.ToString()));
            }
        }

        protected override void OnStop()
        {
            log.Debug(m => m("OnStop Service ..."));
            this.socketServer1.Stop();
            this.socketServer2.Stop();
            //this.socketServer3.Stop();
        }
    }
}
