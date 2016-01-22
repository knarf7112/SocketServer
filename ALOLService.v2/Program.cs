using System.ServiceProcess;
//
using Common.Logging;
using System;
//install
using System.Configuration.Install;
//
using System.Reflection;

namespace ALOLService.v2
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
                        ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                        break;
                    case "--uninstall":
                        ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;
                }
            }
            else
            {
                ServiceBase[] ServicesToRun = new ServiceBase[]
                {
                    new ALOLService_v2()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }

        private static void CurrentDomain_UnhandledException(Object sender, UnhandledExceptionEventArgs e)
        {
            if (e != null && e.ExceptionObject != null)
            {
                log.Error("Error: " + e.ExceptionObject);
            }

        }
    }
}
