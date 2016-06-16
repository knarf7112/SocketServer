using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Net.Sockets;
namespace WebSocketServer.Client
{
    public class StateObject : ISateObject
    {

        private byte[] _buffer;
        private Socket _client;
        private AsyncCallback _callback;

        #region Constructor
        public StateObject()
        {

        }

        public StateObject(byte[] buffer,Socket client, AsyncCallback callbackMethod)
        {
            this._buffer = buffer;
            this._client = client;
            this._callback = callbackMethod;
        }
        #endregion

        /// <summary>
        /// send data or receive buffer
        /// </summary>
        public byte[] Buffer
        {
            get 
            { 
                return this._buffer; 
            }
            set
            {
                this._buffer = value;
            }
        }

        /// <summary>
        /// client socket object
        /// </summary>
        public Socket Client
        {
            get
            {
                return _client;
            }
            set
            {
                this._client = value;
            }
        }

        /// <summary>
        /// 要執行非同步的任務
        /// </summary>
        public AsyncCallback Callback
        {
            get { return _callback; }
            set { this._callback = value; }
        }
    }
}
