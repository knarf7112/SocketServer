using System;

namespace SocketServer.Handlers.State
{
    /// <summary>
    /// 服務選擇工廠
    /// </summary>
    public static class ServiceFactory
    {
        private static ServiceSelect service;
        /// <summary>
        /// 依據設定檔取得的設定字串選擇要新增的服務物件
        /// </summary>
        /// <param name="serviceName">服務名稱</param>
        /// <param name="ignoreCase">忽略服務大小寫</param>
        /// <returns>指定的服務要執行的工作狀態</returns>
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
                case ServiceSelect.LoadKey:
                    return new State_KeyGetter();
                default:
                    return new State_Exit();
            }
        }
    }

    /// <summary>
    /// 服務名稱列表
    /// </summary>
    public enum ServiceSelect
    {
        /// <summary>
        /// 認證服務
        /// </summary>
        Authenticate = 0,
        /// <summary>
        /// 取得Divers key
        /// </summary>
        LoadKey = 1
    }
}
