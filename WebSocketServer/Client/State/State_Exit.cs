using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketServer.Client.State
{
    public class State_Exit : IState
    {
        public void Handle(AbsRequestHandler handler)
        {
            Logger.WriteLog("Client " + handler.ClientNo + " Exit ...");
            //exit state
            handler.CancelAsync();
        }
    }
}
