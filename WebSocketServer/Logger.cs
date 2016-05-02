using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketServer
{
    public delegate void WriteLog(string msg);
    public static class Logger
    {

        #region Event : Write Log
        /// <summary>
        /// 委派給外部去設定寫Log的方法
        /// </summary>
        public static WriteLog OnWriteLog { private get; set; }
        /// <summary>
        /// 若有設定委派方法,則輸出內部寫的Log
        /// </summary>
        /// <param name="msg"></param>
        public static void WriteLog(string msg)
        {
            if (null != OnWriteLog)
            {
                string info = String.Format("{0} [ThreadId:{1}][Method:{2}] ",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Thread.CurrentThread.ManagedThreadId,
                    GetCurrentMethod());
                OnWriteLog.Invoke(info + msg);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetCurrentMethod()
        {
            //ref:http://stackoverflow.com/questions/2652460/c-sharp-how-to-get-the-name-of-the-current-method-from-code
            StackTrace trace = new StackTrace();
            //取呼叫的方法frame(扣掉GetCurrentMethod和WriteLog方法,所以是第2個)
            StackFrame sf = trace.GetFrame(2);

            return sf.GetMethod().Name;
        }
        #endregion
    }
}
