using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Net.Sockets;

namespace WebSocketServer.Client
{
    public interface ISateObject
    {
        /// <summary>
        /// send data or receive buffer
        /// </summary>
        byte[] Buffer { get; }

        /// <summary>
        /// client socket
        /// </summary>
        Socket Client { get; set; }

        /// <summary>
        /// do work method
        /// </summary>
        AsyncCallback Callback { get; }
    }
}
