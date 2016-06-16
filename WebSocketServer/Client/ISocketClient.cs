
namespace WebSocketServer.Client
{
    public interface ISocketClient
    {
        int ClientNo { get; set; }
        /// <summary>
        /// 執行client端Socket非同步送出作業
        /// </summary>
        /// <param name="obj"></param>
        void AsyncSend(ISateObject obj);
        
        /// <summary>
        /// 執行client端Socket非同步接收作業
        /// </summary>
        /// <param name="obj"></param>
        void AsyncReceive(ISateObject obj);
    }
}
