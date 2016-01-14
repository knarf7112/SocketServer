using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketServer.v2
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncMultiSocketServer s1 = new AsyncMultiSocketServer(6112, "QQ");
            Console.WriteLine("Main Thread Id:{0}  background:{1}", Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.IsBackground.ToString());
            s1.Start();
            //
            Console.ReadKey();
            s1.Stop();
            Console.ReadKey();
        }
    }
}
