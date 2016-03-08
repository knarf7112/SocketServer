using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections;

namespace Test_Func
{
    class Program2
    {
        class stateObj
        {
            public Socket sck;
            public int ClientNo;
            public ManualResetEvent conn;
        }
        public static void Main2(string[] args)
        {
            ManualResetEvent conn = new ManualResetEvent(false);
            int count = 0;
            Socket main = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint bindingPoint = new IPEndPoint(IPAddress.Any,6199);
            main.Bind(bindingPoint);
            main.Listen(10);
            Console.WriteLine("main socket status: Blocking:{0}, localIP:{1}", main.Blocking, main.LocalEndPoint.ToString());
            do
            {

                conn.Reset();
                lock (main)
                {
                    count++;
                }
                main.BeginAccept(AceptCallback, new stateObj { sck = main, ClientNo = count, conn = conn });
                Console.WriteLine("wait one : " + count);
                conn.WaitOne();
            }
            while (true);
            Console.ReadKey();
            main.Close(1000);
        }
        static void AceptCallback(IAsyncResult ar)
        {
            try
            {
                if (ar.IsCompleted && ((stateObj)ar.AsyncState).conn.Set())
                {
                    Socket sck = ((stateObj)ar.AsyncState).sck.EndAccept(ar);
                    //((stateObj)ar.AsyncState).conn.Set();
                    String msg = "Hello 測試...client編號:" + ((stateObj)ar.AsyncState).ClientNo;
                    //sck.Send(Encoding.UTF8.GetBytes(msg));
                    sck.Send(Encoding.GetEncoding(950).GetBytes(msg));
                    byte[] buffer = new byte[1000];
                    int size = sck.Receive(buffer);
                    Array.Resize(ref buffer, size);
                    //Console.WriteLine("接受到:" + Encoding.UTF8.GetString(buffer));
                    Console.WriteLine("接受到:" + Encoding.GetEncoding(950).GetString(buffer));
                    sck.Close();
                    sck.Dispose();
                } 
            }
            catch (SocketException sckEx)
            {
                Console.WriteLine("Error:" + sckEx.Message);
            }
        }
    }

    #region Nuances foreach (foreach細微之處)
    class Program
    {
        static void Main(string[] args)
        {
            //ref:http://codingsight.com/%D1%81-nuances-foreach/
            //how to create a iterator
            //1.create a class need GetEnumerator method to return a Enumerator object
            //enumerator is enumerable that has MoveNext method and Current property and implement IDisposable
            //and then can use foreach run enumerator
            var container = new Container();
            foreach (var item in container)
            {

            }

            //2.that inside implement like this
            Enumerator iterator = container.GetEnumerator();
            try
            {
                object element = null;
                while (iterator.MoveNext())
                {
                    element = iterator.Current;
                }
            }
            finally
            {
                IDisposable disposable = iterator as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    class Container
    {
        public Enumerator GetEnumerator()
        {
            return new Enumerator();
        }
    }

    class Enumerator : IDisposable
    {
        public bool MoveNext()
        {
            return false;
        }

        public object Current
        {
            get { return null; }
        }

        public void Dispose()
        {
            Console.WriteLine("Dispose ...");
        }
    }
    #endregion

}
