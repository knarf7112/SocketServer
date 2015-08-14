using Common.Logging;
using System.Net;
//

namespace SocketServer.Handlers.State
{
    /// <summary>
    /// 狀態:離開處理程序並關閉此客戶端
    /// </summary>
    public class State_Exit : IState
    {
        protected static readonly ILog log = LogManager.GetLogger(typeof(State_Exit));

        public void Handle(AbsClientRequestHandler absClientRequestHandler)
        {
            log.Debug("State : Exit => ClientNo:" + absClientRequestHandler.ClientNo);
            absClientRequestHandler.KeepService = false;
            absClientRequestHandler.SocketServer.RemoveClient(absClientRequestHandler.ClientNo);
        }
    }
}
