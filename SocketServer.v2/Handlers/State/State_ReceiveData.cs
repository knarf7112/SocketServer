using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Net.Sockets;

namespace SocketServer.v2.Handlers.State
{
    public class State_ReceiveData : IState
    {
        public void Handle(ClientRequestHandler handler)
        {
            byte[] buffer = new byte[0x1000];
            int receiveLength = 0;
            SocketError errorCode;
            string receiveString = String.Empty;
            Console.WriteLine("State ReceiveData: ClientNo {0} | Count{1}", handler.ClientNo, handler.UseCount);

            receiveLength = handler.ClientSocket.Receive(buffer, 0, buffer.Length, SocketFlags.None, out errorCode);
            if (errorCode == SocketError.Success && receiveLength > 0)
            {
                Array.Resize(ref buffer, receiveLength);
                receiveString = BitConverter.ToString(buffer).Replace("-", "");
                Console.WriteLine("Request:{0}", receiveString);
                handler.ServiceState = new State_Factory(buffer);
            }
            else
            {
                Console.WriteLine("[{0}][Handle]Socket Error: {1}", this.GetType().Name, errorCode.ToString());
                handler.ServiceState = new State_Exit();
            }
        }
    }
}
