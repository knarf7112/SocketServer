using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
//SHA1
using System.Security.Cryptography;
//socket
using System.Net.Sockets;
//regular expression
using System.Text.RegularExpressions;

namespace WebSocketServer.Client.State
{
    public class State_HandShake : IState
    {
        private static readonly string WEBSOCKET_GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private static SHA1 _sha1 = SHA1.Create();
        private static int RECEIVE_LENGTH = 1024;
        private static int RECEIVE_TIMEOUT = 2000;//2 second
        private static int SEND_TIMEOUT = 2000;// 2 second
        public void Handle(AbsRequestHandler handler)
        {
            try
            {
                byte[] buffer = null;
                IEnumerable<byte> receiveBuffer = new byte[0];
                do
                {
                    buffer = new byte[RECEIVE_LENGTH];//length:chrome:514, firefox:533, IE11:289
                    int receiveLength = handler.ClientSocket.Receive(buffer);
                    if(buffer.Length > receiveLength)
                    {
                        Array.Resize(ref buffer, receiveLength);
                    }
                    receiveBuffer = receiveBuffer.Concat(buffer);
                }
                while (handler.ClientSocket.Available > 0);
                byte[] data = receiveBuffer.ToArray();
                Logger.WriteLog("收到資料了...");
                byte[] response = null;
                if (!ResponseHandShake(data, out response)) 
                {
                    handler.ServiceState = new State_Exit();
                    return;
                }
                int sendLength = handler.ClientSocket.Send(response);
                Logger.WriteLog("Response HandShake data and length:" + sendLength.ToString());
                handler.ServiceState = new State_AsyncCommunicate();
            }
            catch(SocketException sckEx)
            {
                Logger.WriteLog("[SocketException]" + sckEx.Message);
                handler.ServiceState = new State_Exit();
            }

        }
        public static bool ResponseHandShake(byte[] data,out byte[] responseData)
        {
            responseData = null;
            try
            {
                Regex reg = new Regex("Sec-WebSocket-Key: (.*)", RegexOptions.IgnoreCase);
                string request = Encoding.UTF8.GetString(data);
                string key = reg.Match(request).Groups[1].Value;
                if (String.IsNullOrEmpty(key))
                {
                    return false;
                }
                //response header
                responseData = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine +
                                                      "Connection: Upgrade" + Environment.NewLine +
                                                      "Upgrade: websocket" + Environment.NewLine +
                                                      "Sec-WebSocket-Accept: " + Convert.ToBase64String(ComputeHash(key.Trim() + WEBSOCKET_GUID)) +
                                                       Environment.NewLine + Environment.NewLine);
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog("HandShake Failed:" + ex.Message);
                return false;
            }

        }

        public static byte[] ComputeHash(string key)
        {
            return _sha1.ComputeHash(Encoding.ASCII.GetBytes(key));
        }
    }
}
