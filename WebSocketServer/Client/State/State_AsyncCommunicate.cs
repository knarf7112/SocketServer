using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//SocketException
using System.Net.Sockets;
//ManualResetEvent
using System.Threading;

namespace WebSocketServer.Client.State
{
    /// <summary>
    /// think:這邊無法非同步 ,因為做完非同步接收與送出後就結束此狀態了,
    /// 會再到Communicate方法那邊繼續運行下個狀態(不會block),又因為不能離開此狀態(離開就等同Client斷線了)
    /// 
    /// </summary>
    public class State_AsyncCommunicate : IState
    {
        private AutoResetEvent _receiveDone = new AutoResetEvent(false);
        private bool _keepAlive;
        private byte[] _maskingKey;
        private int _kindOfLength;
        private int _payload_size;
        private IEnumerable<byte> receiveData;
        public void Handle(AbsRequestHandler handler)
        {
            try
            {
                _keepAlive = true;
                receiveData = new byte[0];
                //this.AsyncReceive(handler);

                do
                {
                    Array.Clear(handler.ReceiveData, 0, handler.ReceiveData.Length);
                    handler.ClientSocket.BeginReceive(handler.ReceiveData, 0, handler.ReceiveData.Length, SocketFlags.None, ReceiveCallback, handler);
                    _receiveDone.WaitOne();
                }
                while (_keepAlive);
            }
            catch (SocketException sckEx)
            {
                Logger.WriteLog("[SocketException]" + sckEx.Message);
            }
            catch (Exception ex)
            {
                Logger.WriteLog("[Exception]" + ex.Message);
            }
            finally
            {
                handler.ServiceState = new State_Exit();
            }
        }

        #region Receive
        protected virtual void ReceiveCallback(IAsyncResult ar)
        {

            try
            {
                AbsRequestHandler clientObj = ar.AsyncState as AbsRequestHandler;
                _receiveDone.Set();
                do
                {
                    int receiveLength = clientObj.ClientSocket.EndReceive(ar);

                    if(receiveLength < clientObj.ReceiveData.Length )
                    {
                        Array.Resize(ref clientObj.ReceiveData, receiveLength);
                    }
                    //lock (receiveData)
                    //{
                        receiveData = receiveData.Concat(clientObj.ReceiveData);
                    //}
                }
                while (clientObj.ClientSocket.Available >= 0);

                string receive = Encoding.UTF8.GetString(receiveData.ToArray());
                Logger.WriteLog("Client " + clientObj.ClientNo + " ReceiveData:" + receive);
                if (receive.Contains("Exit"))
                {
                    _keepAlive = false;
                    _receiveDone.Set();
                }
                byte[] sendData = Encoding.UTF8.GetBytes("Server Say:" + receive);
                clientObj.ClientSocket.BeginSend(sendData, 0, sendData.Length, SocketFlags.None, SendCallback, clientObj.ClientSocket);
                Logger.WriteLog("SendLength:" + sendData.Length + " Server Say:" + receive);
            }
            catch (SocketException sckEx)
            {
                Logger.WriteLog(sckEx.Message);
            }
        }

        protected bool FilterPayloadData(byte[] data,out bool over126,out long payLoad_size)
        {
            over126 = false;
            payLoad_size = 0;
            _maskingKey = new byte[4];//masking-key size
            int length = data[1] & 0x7F;
            if (length <= 125)
            {
                _kindOfLength = 2;//masking-key array start index
                _payload_size = length;
            }
            else if (length == 126)
            {
                //payload length: 126~65535
                _kindOfLength = 4;
                _payload_size = (data[2] << 8) | data[3];//
            }
            else if (length == 127)
            {

                _kindOfLength = 10;
                payLoad_size = 0;
                for (int i = 2; i < _kindOfLength; i++)
                {
                    payLoad_size = payLoad_size + (long)(data[i] << (8 * (_kindOfLength - i - 1)));
                }
                over126 = true;
            }
            else
            {
                return false;
            }
            Buffer.BlockCopy(data, _kindOfLength, _maskingKey, 0, 4);//copy masking-key
            return true;
        }
        #endregion

        #region Send
        protected virtual void AsyncSend(Socket client,byte[] data)
        {
            try
            {
                client.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, client);
                Logger.WriteLog(Encoding.UTF8.GetString(data));
            }
            catch (SocketException sckEx)
            {
                Logger.WriteLog(sckEx.Message);
            }
        }

        protected virtual void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                int sendLength = client.EndSend(ar);

                Logger.WriteLog("[SendCallback] length:" + sendLength.ToString());

            }
            catch (SocketException sckEx)
            {
                Logger.WriteLog(sckEx.Message);
            }
        }
        #endregion
    }
    public class StateObj
    {
        public static readonly int RECEIVE_SIZE = 1024;

        public Socket Client_Sck { get; set; }

        public byte[] ReceiveData = new byte[RECEIVE_SIZE];
    }
}
