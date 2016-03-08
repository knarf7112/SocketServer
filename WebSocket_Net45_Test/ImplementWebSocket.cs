using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Net.WebSockets;

namespace WebSocket_Net45_Test
{
    public class ImplementWebSocket : WebSocket
    {
        private WebSocket _webSocket;
        public override void Abort()
        {
            
            throw new NotImplementedException();
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override WebSocketCloseStatus? CloseStatus
        {
            get { throw new NotImplementedException(); }
        }

        public override string CloseStatusDescription
        {
            get { throw new NotImplementedException(); }
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, System.Threading.CancellationToken cancellationToken)
        {
            WebSocketReceiveResult result = new WebSocketReceiveResult(2000, WebSocketMessageType.Binary, true);
            
            if (cancellationToken.IsCancellationRequested)
            {
                //return buffer
            }
            throw new NotImplementedException();
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override WebSocketState State
        {
            get { throw new NotImplementedException(); }
        }

        public override string SubProtocol
        {
            get { throw new NotImplementedException(); }
        }
    }
}
