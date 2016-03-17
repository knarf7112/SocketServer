using System;
using System.Text;

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
            //ISocketServer s1 = new AsyncMultiSocketServer(6112, "Authenticate");
            //s1.Start();
            ISocketServer s2 = new AsyncMultiSocketServer(6112, "LoadKey");
            s2.Start();
            //ISocketServer s3 = new AsyncMultiSocketServer(6113, "LoadKeyTxLog");
            //s3.Start();
            //ISocketServer s4 = new AsyncMultiSocketServer(6114, "LoadKeyList");
            //s4.Start();
            string cmd = string.Empty;
            while (!cmd.Equals("exit"))
            {
                cmd = Console.ReadLine();
            }
        }
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}
