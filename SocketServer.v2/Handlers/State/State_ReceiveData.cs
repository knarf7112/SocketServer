using System;
//socket
using System.Net.Sockets;
//log
using Common.Logging;

namespace SocketServer.v2.Handlers.State
{
    /// <summary>
    /// 處理狀態: Client Socket 接收端
    /// </summary>
    public class State_ReceiveData : IState
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(State_ReceiveData)); 
        public void Handle(ClientRequestHandler handler)
        {
            byte[] buffer = new byte[0x1000];//buffer 4k size
            int receiveLength = 0;
            SocketError errorCode;
            string receiveString = String.Empty;
            //TODO...若被ping是否步要顯示log?
            //Console.WriteLine("State ReceiveData: ClientNo {0} | Count{1}", handler.ClientNo, handler.UseCount);
            log.Debug(m => m("State ReceiveData: ClientNo {0} | Count:{1}", handler.ClientNo, handler.UseCount));
            try
            {
                receiveLength = handler.ClientSocket.Receive(buffer, 0, buffer.Length, SocketFlags.None, out errorCode);

                if (errorCode == SocketError.Success)
                {
                    if (receiveLength > 0)
                    {
                        Array.Resize(ref buffer, receiveLength);
                        receiveString = BitConverter.ToString(buffer).Replace("-", "");
                        //Console.WriteLine("[{0}]Request:{1}", this.GetType().Name, receiveString);
                        log.Info(m => m("[State_ReceiveData][Handle]Request[hex]:{0}", receiveString));
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
                    //預期情況內的Socket異常
                    //Console.WriteLine("[{0}][Handle]Socket Error: {1}", this.GetType().Name, errorCode.ToString());
                    log.Error(m => m("[State_ReceiveData][Handle]Client Socket Error:{0}", errorCode.ToString()));
                    handler.ServiceState = new State_Exit();
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("超過預期的異常:{0}", ex.Message);
                log.Error(m => m("[State_ReceiveData][Handle]超過預期的異常:{0}", ex.Message));
                handler.ServiceState = new State_Exit();
            }
        }
    }
}
