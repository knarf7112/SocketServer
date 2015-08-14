using System;

namespace SocketServer.Handlers.State
{
    /// <summary>
    /// 服務選擇工廠
    /// </summary>
    public static class ServiceFactory
    {
        private static ServiceSelect service;
        public static IState GetService(string serviceName, bool ignoreCase = false)
        {
            try
            {
                service = (ServiceSelect)Enum.Parse(typeof(ServiceSelect), serviceName, ignoreCase);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            switch (service)
            {
                case ServiceSelect.Authenticate:
                    return new State_Authenticate();

                default:
                    return new State_Exit();
            }
        }
    }

    public enum ServiceSelect
    {
        Authenticate = 0,
    }
}
