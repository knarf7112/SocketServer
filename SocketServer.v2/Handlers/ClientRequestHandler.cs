using System;
using System.Linq;
using System.Text;
//log
using Common.Logging;

namespace SocketServer.v2.Handlers
{
    /// <summary>
    /// 
    /// </summary>
    public class ClientRequestHandler : AbsClientRequestHandler
    {
        #region Static
        private static readonly ILog log = LogManager.GetLogger(typeof(ClientRequestHandler));
        /// <summary>
        /// 控制字元的陣列
        /// </summary>
        private static string[] controlChars =
        {
          "<NUL>" , "<SOH>" , "<STX>" , "<ETX>" ,
          "<EOT>" , "<ENQ>" , "<ACK>" , "<BEL>" ,
          "<BS>"  , "<HT>"  , "<LF>"  , "<VT>"  ,
          "<FF>"  , "<CR>"  , "<SO>"  , "<SI>"  ,
          "<DLE>" , "<DC1>" , "<DC2>" , "<DC3>" ,
          "<DC4>" , "<NAK>" , "<SYN>" , "<ETB>" ,
          "<CAN>" , "<EM>"  , "<SUB>" , "<ESC>" ,
          "<FS>"  , "<GS>"  , "<RS>"  , "<US>"  ,
        };
        /// <summary>
        /// ASCII轉換成字串包含控制字元
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string asciiOctets2String(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            //ref:http://stackoverflow.com/questions/18498149/byte-array-with-control-characters-to-string
            foreach (char ch in bytes.Select(b => (char)b))
            {
                if (ch < '\u0020')
                {
                    sb.Append(ClientRequestHandler.controlChars[ch]);
                }
                else if (ch == '\u007F')
                {
                    sb.Append(sb.Append("<DEL>"));
                }
                else if (ch > '\u007F')
                {
                    sb.AppendFormat(@"\u{0:X4}", (ushort)ch);
                }
                else
                {
                    /* 0x20-0x7E */
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }
        #endregion

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
            //Console.WriteLine("Time Spend:{0}ms", this.timer.ElapsedMilliseconds.ToString());
            log.Info(m => m("Time Spend:{0}ms", this.timer.ElapsedMilliseconds.ToString()));
        }
        /// <summary>
        /// Handler status:只顯示使用狀態與累計次數
        /// </summary>
        /// <returns>handler used status</returns>
        public override string ToString()
        {
            return String.Format("ClientHandlerNo:{0}, 使用總次數:{1}, 使用中:{2}",this.ClientNo.ToString(),this.UseCount.ToString(),this.IsUsed.ToString());
        }

    }
}
