using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketServer.v2.Handlers
{
    public class ClientRequestHandler : AbsClientRequestHandler
    {

        public ClientRequestHandler(int clientNo):base(clientNo)
        {

        }
        /// <summary>
        /// 
        /// </summary>
        public override void DoCommunicate()
        {
            this.ServiceState = new State.State_ReceiveData();
            this.timer.Restart();
            while (this.KeepService)
            {
                this.ServiceState.Handle(this);
            }
            this.timer.Stop();
            Console.WriteLine("Time Spend:{0}ms", this.timer.ElapsedMilliseconds.ToString());
        }

    }
}
