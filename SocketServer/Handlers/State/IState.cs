using SocketServer.Handlers;

namespace SocketServer.Handlers.State
{
    public interface IState 
    {
        /// <summary>
        /// 處理client context狀態的端口
        /// </summary>
        /// <param name="absClientRequestHandler">context of current socket client</param>
        void Handle(AbsClientRequestHandler absClientRequestHandler);
    }
}
