using System;

namespace SocketServer
{
    public class Class1
    {
        /// <summary>
        /// console in
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            ISocketServer s1 = new AsyncMultiSocketServer(6112, "Authenticate");
            s1.Start();

            string cmd = string.Empty;
            while (!cmd.Equals("exit"))
            {
                cmd = Console.ReadLine();
            }
        }
    }
}
