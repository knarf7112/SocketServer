using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Net.WebSockets;
using System.Threading;

namespace WebSocket_Net45_Test
{
    class Program
    {
        //ref:http://readily-notes.blogspot.tw/
        static void Main(string[] args)
        {
            WebSocket sck;

            Console.WriteLine(string.Format("main:{0}", Thread.CurrentThread.ManagedThreadId));

            Task t = Task.Run(() => { Console.WriteLine(String.Format("Task:{0}", Thread.CurrentThread.ManagedThreadId)); });

            qq();


            Console.WriteLine("end ...");
            Console.ReadKey();
        }

        static async Task GetAwaitThreadId()
        {
            Thread.Sleep(3000);
            //Await方法執行緒ID
            Console.WriteLine(string.Format("Await:{0}", System.Threading.Thread.CurrentThread.ManagedThreadId));
        }
        //async await 並不會產生新的執行緒
        static async Task qq()
        {
            await GetAwaitThreadId();
        }
    }
}
