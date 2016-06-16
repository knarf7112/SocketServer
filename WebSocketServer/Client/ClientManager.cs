using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketServer.Client
{
    public class ClientManager
    {
        #region Field
        private static ClientManager _instance;

        #endregion

        #region Properties
        protected IDictionary<string, ISocketClient> dic_socketClient;

        #endregion

        private ClientManager()
        {
            //init object
        }

        public ClientManager GetInstance()
        {
            if (null == _instance) 
            lock(_instance)
            {
                if (null == _instance)
                {
                    _instance = new ClientManager();
                }
            }
            return _instance;
        }



    }
}
