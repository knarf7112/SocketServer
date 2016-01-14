using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketServer.v2
{
    /// <summary>
    /// 服務選擇工廠
    /// </summary>
    public static class ServiceFactory
    {
       // public static I
       
    }
    /// <summary>
    /// 依據Port分狀態列表
    /// </summary>
    public enum ServiceType
    {
        /// <summary>
        ///  none 
        /// </summary>
        None = 0,
        /// <summary>
        /// 自動加值服務 ConType: 0332[自動加值], 0333[自動加值TxLog], 0334[自動加值沖正TxLog]
        /// </summary>
        AutoLoad = 1,
        /// <summary>
        /// 自動加值查詢服務 ConType: 0531[自動加值查詢]
        /// </summary>
        AutoLoadQuery = 2,
        /// <summary>
        /// 一般加値和購貨取消服務 ConType: 0331[一般加値],0341[一般加値TxLog]  ; 0631[購貨取消], 0641[購貨取消Txlog]
        /// </summary>
        LoadAndPurshaseReturn = 3
    }
}
