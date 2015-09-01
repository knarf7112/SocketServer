using SocketServer.Handlers.State;
using System.ComponentModel;
using System.Net.Sockets;

namespace SocketServer.Handlers
{
    public class ClientRequestHandler : AbsClientRequestHandler
    {
        #region Constructor
        public ClientRequestHandler(int clientNo,Socket socket4Client,ISocketServer socketServer,IState state)
        {
            //init backgroundworker
            this.backgroundWorker = new BackgroundWorker()
            {
                WorkerSupportsCancellation = true
            };
            this.backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            this.backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
            //init variable
            this.ClientNo = clientNo;
            this.ClientSocket = socket4Client;
            this.ClientSocket.SendTimeout = this.writeTimeout;
            this.ClientSocket.ReceiveTimeout = this.readTimeout;
            this.SocketServer = socketServer;//link from parent socket server
            this.KeepService = true;
            this.ServiceState = state;
            //init logger
            //if (log == null)
            //{
            //    log = LogManager.GetLogger(typeof(ClientRequestHandler));
            //}
        }

        public ClientRequestHandler(int clientNo, Socket socket4Client, ISocketServer socketServer):this(clientNo,socket4Client,socketServer,new State_Authenticate())
        {

        }
        public ClientRequestHandler()
            : this(-1, null, null)
        {

        }
        #endregion
    }
}
