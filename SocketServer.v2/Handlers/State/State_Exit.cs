using System;

namespace SocketServer.v2.Handlers.State
{
    public class State_Exit :IState
    {
        public void Handle(ClientRequestHandler handler)
        {
            Console.WriteLine("Exit State ...");
            
            handler.CancelAsync();
        }
    }
}
