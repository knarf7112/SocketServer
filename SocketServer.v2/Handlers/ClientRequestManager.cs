using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Collections.Concurrent;

namespace SocketServer.v2.Handlers
{
    /// <summary>
    /// 
    /// </summary>
    public class ClientRequestManager
    {
        #region Field
        /// <summary>
        /// Client集合
        /// </summary>
        private IDictionary<int, ClientRequestHandler> dic;

        private object lockObj = new object();
        #endregion

        public int ClientNo { get; set; }

        /// <summary>
        /// 異常列表
        /// </summary>
        public IList<string> ErrorList { get; set; }

        public ClientRequestManager()
        {
            this.dic = new ConcurrentDictionary<int, ClientRequestHandler>();
            this.ErrorList = new List<string>();
        }
        /// <summary>
        /// 取得一個未使用的client handler
        /// </summary>
        /// <returns></returns>
        public IClientRequestHandler GetInstance()
        {
            IClientRequestHandler result = null;
            lock (lockObj)
            {
                //get one client object that is not using, if none then create new 
                result = this.dic.Where(n => { return n.Value.IsUsed == false; }).FirstOrDefault().Value;
                //沒有就建一個新的ClientHandler
                if (result == null)
                {
                    this.ClientNo++;
                    result = new ClientRequestHandler(this.ClientNo);
                    this.dic.Add(this.ClientNo, (ClientRequestHandler)result);
                }
                ((ClientRequestHandler)result).IsUsed = true;
            }

            return result;
        }

        /// <summary>
        /// 取得目前client handler的總量
        /// </summary>
        /// <returns></returns>
        public int GetCount()
        {
            return this.dic.Count;
        }

        /// <summary>
        /// 列表目前所有client的Client NO,使用次數,是否正在使用
        /// </summary>
        /// <returns>client狀態列表</returns>
        public IEnumerable<String> GetClientStatus()
        {
            foreach (ClientRequestHandler client in this.dic.Values)
            {
                yield return client.ToString();
            }
        }

        /// <summary>
        /// 清除所有client handler
        /// </summary>
        public void ClearAll()
        {
            this.dic.Clear();
            this.ErrorList.Clear();
            this.ClientNo = 0;
        }

        /// <summary>
        /// 指定client Id移除client handler
        /// </summary>
        /// <param name="clientNo">指定client Id</param>
        public void RemoveClient(int clientNo)
        {
            if (this.dic.ContainsKey(clientNo))
            {
                lock (this.lockObj)
                {
                    if (!this.dic[clientNo].IsUsed)
                    {
                        bool removed = this.dic.Remove(clientNo);
                        if (!removed)
                        {
                            this.ErrorList.Add("Client Dictionary Error: remove " + clientNo + " failed");
                        }
                    }
                    else
                    {
                        this.ErrorList.Add("Client Dictionary Error: clientNo " + clientNo + " is using, can't remove");
                    }
                }
            }
            else
            {
                this.ErrorList.Add("Client Dictionary: clientNo " + clientNo + " not exists!");
            }
        }
    }
}
