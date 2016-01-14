using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketServer.v2.Handlers.State
{
    public class State_AutoLoad : IState
    {
        public void Handle(ClientRequestHandler handler)
        {
            try
            {
                string testStr = "hi, this is State_AutoLoad";
                Console.WriteLine("[{0}] RequestString:\r\n{1}", this.GetType().Name, handler.Request);
                byte[] sendBytes = Encoding.ASCII.GetBytes(testStr);
                handler.ClientSocket.Send(sendBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[{0}][Handle] Error: {0} \r\n{1}", this.GetType().Name, ex.Message, ex.StackTrace);
            }
            finally
            {
                handler.ServiceState = new State_Exit();
            }
        }
    }
}
