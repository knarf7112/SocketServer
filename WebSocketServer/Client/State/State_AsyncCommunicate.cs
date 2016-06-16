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
        #region WebSocket Component
        private AutoResetEvent _receiveDone = new AutoResetEvent(false);
        private byte[] _maskingKey { get; set; }
        private int _kindOfLength { get; set; }
        private int _payload_size { get; set; }
        private IEnumerable<byte> receiveData { get; set; }
        private bool _keepAlive;
        private int _sequenceNO;//StateObj產生的順序
        private AbsRequestHandler _clientHandler;
        #endregion

        public void Handle(AbsRequestHandler handler)
        {
            try
            {
                this._clientHandler = handler;
                _keepAlive = true;
                receiveData = new byte[0];
                //this.AsyncReceive(handler);
                handler.ReceiveData = new byte[20];
                do
                {
                    lock (_clientHandler)
                    {
                        _sequenceNO += 1; 
                    }
                    StateObj obj = new StateObj { SequnceNO = _sequenceNO, ClientSocket = handler.ClientSocket };
                    handler.ClientSocket.BeginReceive(obj.Receive_buffer, 0, obj.Receive_buffer.Length, SocketFlags.None, ReceiveCallback, obj);
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
                StateObj clientObj = ar.AsyncState as StateObj;
                _receiveDone.Set();
                
                    int receiveLength = clientObj.ClientSocket.EndReceive(ar);

                    if(receiveLength < clientObj.Receive_buffer.Length )
                    {
                        Array.Resize(ref clientObj.Receive_buffer, receiveLength);
                    }
                    //lock (receiveData)
                    //{
                        //receiveData = receiveData.Concat(clientObj.ReceiveData);
                    //}
                    if (clientObj.ClientSocket.Available > 0)
                    {
                        receiveData = receiveData.Concat(clientObj.Receive_buffer);
                    }
                    else
                    {
                        //判斷websocket frame格式
                        bool over126 = false;
                        long payloadSize_over126;
                        if (null == _maskingKey && !FilterPayloadData(receiveData.ToArray(), out over126, out payloadSize_over126))
                        {
                            _clientHandler.CancelAsync();
                        }

                        string receive = Encoding.UTF8.GetString(receiveData.ToArray());
                        Logger.WriteLog("Client " + clientObj.SequnceNO + " ReceiveData:" + receive);
                        if (receive.Contains("Exit"))
                        {
                            _keepAlive = false;
                            _receiveDone.Set();
                        }
                        byte[] sendData = Encoding.UTF8.GetBytes("Server Say:" + receive);
                        clientObj.ClientSocket.BeginSend(sendData, 0, sendData.Length, SocketFlags.None, SendCallback, clientObj.ClientSocket);
                        Logger.WriteLog("SendLength:" + sendData.Length + " Server Say:" + receive);
                    }
            }
            catch (SocketException sckEx)
            {
                Logger.WriteLog(sckEx.Message);
            }
        }

        protected bool CheckWebSocketFrame(byte[] data)
        {
            if ((data[0] & 0x88) == 0x88) return false;
            if ((data[1] & 0x80) != 0x80) return false;


            return true;
            
        }

        /// <summary>
        /// 取得masking-key數據(收到的資料要用此來解)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="over126">length大於126(即127)</param>
        /// <param name="payLoad_size">長度65536以上,最多2^63所以用long(規定MSB為0,所以是2^(64-1))</param>
        /// <returns>true/false:</returns>
        protected bool FilterPayloadData(byte[] data,out bool over126,out long payLoad_size)
        {
            over126 = false;
            payLoad_size = 0;
            _maskingKey = new byte[4];//masking-key size
            int length = data[1] & 0x7F;//length and
            if ((data[1] & 0x80) != 0x80) return false;//none masking-key
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

        /// <summary>
        /// 把要回送的數據封裝成websocket規定的frame格式
        /// </summary>
        /// <param name="sendData"></param>
        /// <returns></returns>
        protected byte[] Package(byte[] sendData)
        {
            List<byte> result = new List<byte>();

            result.Add((byte)0x81);//FIN + opcode //byte[0]

            //add data length format
            if (sendData.LongLength <= 125)
            {
                result.Add((byte)sendData.Length);//byte[1]
            }
            else if(sendData.LongLength  <= 65535)
            {
                //前4byte依據websocket frame規格
                result.Add((byte)126);//byte[1]
                result.Add((byte)((sendData.Length >> 8) & 0xFF));//(sendData.Length / 256) //byte[2]
                result.Add((byte)(sendData.Length & 0xFF));       //(sendData.Length % 256) //byte[3]
            }
            else
            {
                //前10byte依據websocket frame規格
                result.Add((byte)127);//byte[1]
                result.Add((byte)((sendData.LongLength >> 8 * 7) & 0xFF));  //byte[2]
                result.Add((byte)((sendData.LongLength >> 8 * 6) & 0xFF));  //byte[3]
                result.Add((byte)((sendData.LongLength >> 8 * 5) & 0xFF));  //byte[4]
                result.Add((byte)((sendData.LongLength >> 8 * 4) & 0xFF));  //byte[5]
                result.Add((byte)((sendData.LongLength >> 8 * 3) & 0xFF));  //byte[6]
                result.Add((byte)((sendData.LongLength >> 8 * 2) & 0xFF));  //byte[7]
                result.Add((byte)((sendData.LongLength >> 8) & 0xFF));      //byte[8]
                result.Add((byte)((sendData.LongLength) & 0xFF));           //byte[9]
            }
            //串上真正的資料
            result.AddRange(sendData);
            return result.ToArray();;
        }
        #endregion
    }

    protected class StateObj
    {
        static int BUFFER_SIZE = 1024;

        public int SequnceNO { get; set; }

        public byte[] Receive_buffer = new byte[BUFFER_SIZE];
        
        public Socket ClientSocket { get; set; }

        
    }
}
