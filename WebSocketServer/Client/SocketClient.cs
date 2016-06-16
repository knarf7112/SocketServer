using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Net.Sockets;

namespace WebSocketServer.Client
{
    /// <summary>
    /// Socket Client 物件
    /// </summary>
    public class SocketClient : ISocketClient,IDisposable
    {
        public static readonly int BUFFER_SIZE = 1024;
        private Socket _client;
        private byte[] receiveBuffer = new byte[BUFFER_SIZE];

        public AsyncCallback SendCallback { get; set; }

        public AsyncCallback ReceiveCallback { get; set; }

        /// <summary>
        /// Client Socket Identity
        /// </summary>
        public int ClientNo
        {
            get;
            set;
        }

        #region Constructor
        public SocketClient(Socket client)
        {
            this._client = client;
        }

        #endregion

        /// <summary>
        /// 非同步送出資料並執行SendCallback屬性的委派
        /// </summary>
        /// <param name="data"></param>
        public virtual void AsyncSend(byte[] data)
        {
            try
            {
                if(null == this.SendCallback){
                    throw new NullReferenceException("SendCallback屬性無指定委派方法");
                }
                this._client.BeginSend(data, 0, data.Length, SocketFlags.None, this.SendCallback, _client);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// 非同步接收資料並執行ReceiveCallback
        /// </summary>
        /// <param name="buffer"></param>
        public virtual void AsyncReceive()
        {
            try
            {
                if (null == this.ReceiveCallback)
                {
                    throw new NullReferenceException("ReceiveCallback屬性無指定委派方法");
                }
                if (null == receiveBuffer)
                {
                    receiveBuffer = new byte[0x1000];
                }
                this._client.BeginSend(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, this.ReceiveCallback, _client);
            }
            catch
            {
                throw;
            }
        }

        #region 外部StateObject物件帶入執行callback動作
        /// <summary>
        /// 非同步送出
        /// </summary>
        /// <param name="sendObj"></param>
        public virtual void AsyncSend(ISateObject sendObj)
        {
            try
            {

                if (null == sendObj.Callback)
                {
                    throw new NullReferenceException("[Parameter] Callback Property can't null ");
                }
                if (null == sendObj.Client)
                    sendObj.Client = this._client;

                _client.BeginSend(sendObj.Buffer, 0, sendObj.Buffer.Length, SocketFlags.None, sendObj.Callback, sendObj.Client);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// 非同步接收
        /// </summary>
        /// <param name="SendObj"></param>
        public virtual void AsyncReceive(ISateObject receiveObj)
        {
            try
            {
                if (null == receiveObj.Callback)
                {
                    throw new NullReferenceException("[Parameter] Callback Property can't null");
                }
                if (null == receiveObj.Client)
                    receiveObj.Client = this._client;
                
                _client.BeginReceive(receiveObj.Buffer, 0, receiveObj.Buffer.Length, SocketFlags.None, receiveObj.Callback, receiveObj);
            }
            catch
            {
                throw ;
            }
        }
        #endregion

        public void Dispose()
        {
            if (null != this._client)
            {
                try
                {
                    this._client.Shutdown(SocketShutdown.Both);
                    this._client.Close();
                }
                catch (Exception ex)
                {
                    this.Dispose();
                    Logger.WriteLog(ex.Message);
                }
                finally
                {
                    this._client = null;
                }
            }
        }
    }
}
