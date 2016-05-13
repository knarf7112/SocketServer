using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//StackTrace
using System.Diagnostics;

namespace WebSocketServer.Utility
{

    public class Logger
    {
        public delegate void WriteMsg(string msg);

        public static WriteMsg OnWriteLog { get; set; }

        public static void WriteLog(string msg)
        {
            if (null != OnWriteLog)
            {
                //need lock?not sure
                //lock (OnWriteLog)
                //{
                    string methodName = "[" + WhoCallMe() + "]";
                    OnWriteLog.Invoke(methodName + msg);
                //}
            }
            else
            {
                throw new NullReferenceException("[Logger]OnWriteLog無指定委派方法");
            }
        }

        protected static string WhoCallMe(int index = 2) 
        {
            StackTrace sf = new StackTrace();
            StackFrame frame = sf.GetFrame(index);
            return frame.GetType().Name + "." + frame.GetMethod().Name;
        }
    }
}
