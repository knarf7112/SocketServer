
namespace SocketServer.v2.Handlers
{
    /// <summary>
    /// ClientHandler介面
    /// </summary>
    public interface IClientRequestHandler
    {
        /// <summary>
        /// 啟動 server 和 client 間相互進行通訊
        /// </summary>
        void DoCommunicate();
        /// <summary>
        /// 中斷 server 和 client 間相互進行通訊
        /// </summary>
        void CancelAsync();
    }
}
