using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//

namespace Test_Func.Test
{
    delegate string AsyncMethod();
    /// <summary>
    /// 非同步作業 IAsyncResult / AsyncCallback
    /// ref:https://dotblogs.com.tw/yc421206/2011/01/03/20540
    /// </summary>
    class TestIAsyncResult
    {
        public void CallAsyncMethod()
        {
            AsyncMethod async = new AsyncMethod(TestMethod);
            IAsyncResult ar = async.BeginInvoke(null, null);//不丟callback method,在此處理
            DateTime startTime = DateTime.Now;
            Console.WriteLine("[CallAsyncMethod] ThreadId:{0}, startTime:{1}", System.Threading.Thread.CurrentThread.ManagedThreadId, startTime.ToString("HH:mm:ss.fff"));
            //輪詢非同步的作業狀態
            while (!ar.IsCompleted)
            {
                //Console.WriteLine("...Control closed:{0} , Control valid:{1}", ar.AsyncWaitHandle.SafeWaitHandle.IsClosed, ar.AsyncWaitHandle.SafeWaitHandle.IsInvalid);
                Console.Write(".");
                System.Threading.Thread.Sleep(10);
            }
            Console.WriteLine();
            //結束非同步方法的呼叫並回傳結果
            string result = async.EndInvoke(ar);
            DateTime endTime = DateTime.Now;
            Console.WriteLine("Complete EndTime:{1} TimeSpend:{2}ms and Result:{0}", result, endTime.ToString("HH:mm:ss.fff"), (endTime - startTime).TotalMilliseconds);

        }

        protected string TestMethod()
        {
            Console.WriteLine("[TestMethod] ThreadId:{0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
            long result = 0;
            //大概耗費20秒左右,Test CPU:i5-6500
            for (long i = 0; i < 10000000000; i++)
            {
                result += 1;
            }
            
            //System.Threading.Thread.Sleep(100);
            
            return result.ToString("###,###,###,###,##0");
        }
    }
}
