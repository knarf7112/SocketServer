using System;
using System.ServiceProcess;
//
using Common.Logging;
using System.Configuration.Install;
using System.Reflection;

namespace AuthenticateService
{
    static class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            if (System.Environment.UserInteractive)
            {
                string parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });//取得
                        break;
                    case "--uninstall":
                        ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;

                }
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                   new AuthenticateService()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if(e != null && e.ExceptionObject != null)
            {
                log.Error(m => m("[Program] UnhandledException:{0}", e.ExceptionObject.ToString()));
            }
            
        }
    }
}
