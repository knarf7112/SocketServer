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

        public ClientRequestHandler(int clientNo, bool hasTimeout = true)
            : base(clientNo, hasTimeout)
        {

        }
        /// <summary>
        /// 執行工作流程
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

        public override string ToString()
        {
            return String.Format("ClientHandlerNo:{0}, 使用總次數:{1}, 使用中:{2}",this.ClientNo.ToString(),this.UseCount.ToString(),this.IsUsed.ToString());
        }

    }
}
