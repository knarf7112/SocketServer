using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncMultiSocket s1 = new AsyncMultiSocket(6111, "TestAsyncSocketServer");
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException; //攔截主socket設為null時,beginAccept方法拋出
            s1.OnWriteLog = (string msg) => { Console.WriteLine(msg); };
            s1.Start();

            Console.Read();
            s1.Stop();
            Console.ReadKey();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("超乎預期的異常:" + e.ExceptionObject);
        }
    }
}
