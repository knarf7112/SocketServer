using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Net;
using System.Configuration;

namespace SocketServer.v2.Handlers
{
    /// <summary>
    /// 後台設定檔靜態物件
    /// </summary>
    public class BackEndSetting
    {
        //存放數據的字典檔
        private static IDictionary<ConType, UriBuilder> backEndSettingDic;
        //是否載入過設定檔
        public static bool IsLoad = false;

        /// <summary>
        /// static constructor
        /// 當有new此物件前或訪問此物件的靜態成員,才會執行static constructor,每種Type只會跑一次
        /// (PS:異常會造成unhandled exception [OS:Application執行期間此物件類型就廢了])
        /// </summary>
        static BackEndSetting()
        {
            //Console.Write("跑靜態類別");
            backEndSettingDic = new Dictionary<ConType, UriBuilder>();
            if (!IsLoad)
            {
                LoadSetting();
            }
        }
        /// <summary>
        /// 從設定檔載入指定的Uri格式
        /// </summary>
        protected static void LoadSetting()
        {
            ConType type;
            foreach(var item in ConfigurationManager.AppSettings.AllKeys){
                if (Enum.TryParse(item, true,out type) && !String.IsNullOrEmpty(ConfigurationManager.AppSettings[item]))
                {
                    UriBuilder uri = new UriBuilder(ConfigurationManager.AppSettings[item]);
                    backEndSettingDic.Add(type, uri);
                }
            }
            IsLoad = true;
        }

        /// <summary>
        /// 取得服務的Uri設定數據
        /// </summary>
        /// <param name="type">指定的服務名稱</param>
        /// <returns>Uri設定數據</returns>
        public static UriBuilder GetUri(ConType type)
        {
            if (backEndSettingDic.ContainsKey(type))
            {
                return backEndSettingDic[type];
            }
            return null;
        }
    }
    /// <summary>
    /// 服務分類
    /// </summary>
    public enum ConType
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
        /// 自動加值TxLog服務 ConType: 0333
        /// </summary>
        AutoLoadTxLog = 2,
        /// <summary>
        /// 自動加值沖正TxLog服務 ConType: 0334
        /// </summary>
        AutoLoadReversalTxLog = 3,
        /// <summary>
        /// 自動加值查詢服務 ConType: 0531
        /// </summary>
        AutoLoadQuery = 4,
        /// <summary>
        /// 一般加値服務 ConType: 0331
        /// </summary>
        Loading = 5,
        /// <summary>
        /// 一般加値TxLog服務 ConType: 0341
        /// </summary>
        LoadingTxLog = 6,
        /// <summary>
        /// 購貨取消服務 ConType: 0631
        /// </summary>
        PurchaseReturn = 7,
        /// <summary>
        /// 購貨取消TxLog服務 ConType: 0641
        /// </summary>
        PurchaseReturnTxLog = 8,
    }
}
