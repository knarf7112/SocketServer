using System;
//
using System.Net.Sockets;

namespace SocketServer.v2.Handlers.State
{
    public class State_ReceiveData : IState
    {
        public void Handle(ClientRequestHandler handler)
        {
            byte[] buffer = new byte[0x1000];//buffer 4k size
            int receiveLength = 0;
            SocketError errorCode;
            string receiveString = String.Empty;
            //TODO...若被ping是否步要顯示log?
            Console.WriteLine("State ReceiveData: ClientNo {0} | Count{1}", handler.ClientNo, handler.UseCount);

            receiveLength = handler.ClientSocket.Receive(buffer, 0, buffer.Length, SocketFlags.None, out errorCode);
            if (errorCode == SocketError.Success)
            {
                if (receiveLength > 0)
                {
                    Array.Resize(ref buffer, receiveLength);
                    receiveString = BitConverter.ToString(buffer).Replace("-", "");
                    Console.WriteLine("Request:{0}", receiveString);
                    handler.ServiceState = new State_Factory(buffer);
                }
                else
                {
                    //收到長度0的資料
                    handler.ServiceState = new State_Exit();
                }
            }
            else
            {
                Console.WriteLine("[{0}][Handle]Socket Error: {1}", this.GetType().Name, errorCode.ToString());
                handler.ServiceState = new State_Exit();
            }
        }
    }
}
