﻿using System;
using System.Collections.Generic;
//
using System.Configuration;

namespace SocketServer.v2.Handlers
{
    /// <summary>
    /// 後台設定檔靜態物件
    /// </summary>
    public class ConfigLoader
    {
        //存放數據的字典檔
        //private static IDictionary<ConType, UriBuilder> backEndSettingDic;
        private static IDictionary<ConType, string> backEndStringSettingDic;
        /// <summary>
        /// 是否載入過設定檔
        /// </summary>
        public static bool IsLoaded = false;

        /// <summary>
        /// static constructor
        /// 當有new此物件前或訪問此物件的靜態成員,才會執行static constructor,每種Type只會跑一次
        /// (PS:異常會造成unhandled exception [OS:Application執行期間此物件類型就廢了])
        /// </summary>
        static ConfigLoader()
        {
            //Console.Write("跑靜態類別");
            //backEndSettingDic = new Dictionary<ConType, UriBuilder>();
            backEndStringSettingDic = new Dictionary<ConType, string>();
            if (!IsLoaded)
            {
                LoadSetting();
            }
        }
        /// <summary>
        /// 從設定檔載入指定的連線資訊
        /// </summary>
        protected static void LoadSetting()
        {
            ConType type;
            foreach(var item in ConfigurationManager.AppSettings.AllKeys){
                if (Enum.TryParse(item, true,out type) && !String.IsNullOrEmpty(ConfigurationManager.AppSettings[item]))
                {
                    //UriBuilder uri = new UriBuilder(ConfigurationManager.AppSettings[item]);//非uri格式會拋異常,算了,自己切好了
                    //backEndSettingDic.Add(type, uri);
                    backEndStringSettingDic.Add(type, ConfigurationManager.AppSettings[item].ToString().Trim());
                }
            }
            IsLoaded = true;
        }
        /// <summary>
        /// 取得設定檔數據
        /// </summary>
        /// <param name="type">服務分類</param>
        /// <returns>後台連線字串</returns>
        public static string GetSetting(ConType type)
        {
            if (backEndStringSettingDic.ContainsKey(type))
            {
                return backEndStringSettingDic[type];
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
        /// <summary>
        /// PAM取額度服務 ConType: 0322
        /// </summary>
        PAMQuota = 9
    }
}
