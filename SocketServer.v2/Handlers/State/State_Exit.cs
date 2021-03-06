﻿using System;
//log
using Common.Logging;
namespace SocketServer.v2.Handlers.State
{
    /// <summary>
    /// 處理狀態: close Handler
    /// </summary>
    public class State_Exit :IState
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(State_Factory));
        public void Handle(ClientRequestHandler handler)
        {
            //Console.WriteLine("Exit State ...");
            log.Debug(m => m("Exit State ..."));
            handler.CancelAsync();
        }
    }
}
