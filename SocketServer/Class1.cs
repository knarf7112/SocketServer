using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

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
            ISocketServer s1 = new AsyncMultiSocketServer(6111, "Authenticate");
            s1.Start();

            string cmd = string.Empty;
            while (!cmd.Equals("exit"))
            {
                cmd = Console.ReadLine();
            }
        }
    }
}
