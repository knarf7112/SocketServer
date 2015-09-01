using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocketServer.Handlers.State
{
    public class State_Authenticate : IState
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="absClientRequestHandler"></param>
        public void Handle(AbsClientRequestHandler absClientRequestHandler)
        {
            var qq = absClientRequestHandler;
            absClientRequestHandler.ServiceState = new State_Exit();
        }
    }
}
