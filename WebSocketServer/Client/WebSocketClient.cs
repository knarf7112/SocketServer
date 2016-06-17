using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Net.Sockets;

namespace WebSocketServer.Client
{
    public class WebSocketClient : SocketClient
    {
        public WebSocketClient(Socket client) : base(client) { }

        public virtual void SetAuthentication()
        {
            throw new NotImplementedException();
        }
    }
}
