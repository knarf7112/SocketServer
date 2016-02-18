
namespace SocketServer.v2.Handlers.State
{
    /// <summary>
    /// 處理狀態的介面
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// 處理client context狀態的端口
        /// </summary>
        /// <param name="handler">context of current socket client</param>
        void Handle(ClientRequestHandler handler);
    }
}
