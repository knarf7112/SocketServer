using System;
using System.Collections.Generic;
//
using System.Configuration;
using Common.Logging;

namespace ReaderShipmentWebHandler
{
    /// <summary>
    /// 存放設定檔數據用的靜態物件
    /// </summary>
    public static class ConfigGetter
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ConfigGetter));

        private static IDictionary<string, string> _dicConfig;

        private static object lockObj = new object();

        /// <summary>
        /// static initial config
        /// </summary>
        static ConfigGetter()
        {
            log.Debug((m) => { m.Invoke("初始化設定檔數據載入"); });
            if (_dicConfig == null)
            {
                lock (lockObj)
                {
                    if (_dicConfig == null)
                    {
                        InitialConfig();
                    }
                }
            }
        }

        /// <summary>
        /// 指定Key取得字典檔內的設定資料
        /// </summary>
        /// <param name="configName"></param>
        /// <returns></returns>
        public static string GetValue(string configName)
        {
            log.Debug((m) => { m.Invoke("開始取得設定資料物件:" + configName); });
            //若為null就重新初始化字典檔(double Thread safe check the dictionary field)
            if (_dicConfig == null)
            {
                lock (lockObj)
                {
                    if (_dicConfig == null)
                    {
                        InitialConfig();
                    }
                }
            }
            if (!_dicConfig.ContainsKey(configName))
            {
                log.Debug((m) => { m.Invoke("字典檔Key: " + configName + " 不存在"); });
                return null;
            }

            return _dicConfig[configName];
        }

        /// <summary>
        /// 初始化靜態字典檔並將Config檔內所有的AppSetting數據載入字典檔內
        /// </summary>
        private static void InitialConfig()
        {
            log.Debug((m) => { m.Invoke("開始載入Web.Config的AppSettings設定檔"); });
            _dicConfig = new Dictionary<string, string>();
            try
            {
                foreach (string item in ConfigurationManager.AppSettings.Keys)
                {
                    string configValue = ConfigurationManager.AppSettings[item];

                    _dicConfig.Add(item, configValue);
                }
            }
            catch (Exception ex)
            {
                log.Debug((m) => { m.Invoke("Web設定檔載入資料錯誤:" + ex.Message + "\n " + ex.StackTrace); });
            }

        }
    }
}

