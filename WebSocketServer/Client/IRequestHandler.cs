using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketServer.Client
{
    /// <summary>
    /// ClientHandler介面
    /// </summary>
    public interface IRequestHandler
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
