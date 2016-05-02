using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketServer.Client.State
{
    /// <summary>
    /// 處理狀態的介面
    /// </summary>
    public interface IState
    {
        void Handle(AbsRequestHandler handler);
    }
}
