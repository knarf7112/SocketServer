//Log
using Common.Logging;
//Container
using Spring.Context;
using Spring.Context.Support;
//Msg Parser
using Kms2.Crypto.Common;
using Kms2.Crypto;
using Kms2.Crypto.Utility;

namespace MacGenerator
{
    internal static class MsgContainer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MsgContainer));

        #region 狀態共用的部分
        /// <summary>
        /// get Spring Container
        /// </summary>
        private static IApplicationContext ctx = ContextRegistry.GetContext();
        /// <summary>
        /// Hex Converter Object
        /// </summary>
        public static IHexConverter HexConverter = ctx["hexConverter"] as IHexConverter;
        /// <summary>
        /// Byte Worker Object
        /// </summary>
        public static IByteWorker ByteWorker = ctx["byteWorker"] as IByteWorker;
        /// <summary>
        /// Mac2Manager Object:處理mac用
        /// </summary>
        public static IMac2Manager Mac2Manager = ctx["mac2Manager"] as IMac2Manager;
        /// <summary>
        /// Hash Worker Object:處理hash
        /// </summary>
        public static IHashWorker HashWorker = ctx["hashWorker"] as IHashWorker;
        /// <summary>
        /// 
        /// </summary>
        public static IStrHelper StrHelper = ctx["strHelper"] as IStrHelper;
        #endregion

        #region 狀態指定的部分
        /// <summary>
        /// MsgUtility Object : 設定自動加值的Request電文轉換用的物件(0332)
        /// </summary>
        public static IMsgUtility AOLReqMsgUtility = ctx["icash2AOLReqMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定自動加值的Response電文轉換用的物件(0332)
        /// </summary>
        public static IMsgUtility AOLRespMsgUtility = ctx["icash2AOLRespMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定自動加值TxLog的Request電文轉換用的物件(0333)
        /// </summary>
        public static IMsgUtility TOLReqMsgUtility = ctx["icash2TOLReqMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定自動加值TxLog的Response電文轉換用的物件(0333)
        /// </summary>
        public static IMsgUtility TOLRespMsgUtility = ctx["icash2TOLRespMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定自動加值查詢的Request電文轉換用的物件(0531)
        /// </summary>
        public static IMsgUtility ALQReqMsgUtility = ctx["icash2ALQReqMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定自動加值查詢的Response電文轉換用的物件(0531)
        /// </summary>
        public static IMsgUtility ALQRespMsgUtility = ctx["icash2ALQRespMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定一般加值的Request電文轉換用的物件(0331)
        /// </summary>
        public static IMsgUtility LOLReqMsgUtility = ctx["icash2LOLReqMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定一般加值的Response電文轉換用的物件(0331)
        /// </summary>
        public static IMsgUtility LOLRespMsgUtility = ctx["icash2LOLRespMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定一般加值TxLog的Request電文轉換用的物件(0341)
        /// </summary>
        public static IMsgUtility LOLTxLogReqMsgUtility = ctx["icash2TOLReqMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定一般加值TxLog的Response電文轉換用的物件(0341)
        /// </summary>
        public static IMsgUtility LOLTxLogRespMsgUtility = ctx["icash2TOLRespMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定購貨取消的Request電文轉換用的物件(0631)
        /// </summary>
        public static IMsgUtility PRReqMsgUtility = ctx["icash2POLReqMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定購貨取消的Response電文轉換用的物件(0631)
        /// </summary>
        public static IMsgUtility PRRespMsgUtility = ctx["icash2POLRespMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定購貨取消TxLog的Request電文轉換用的物件(0641)
        /// </summary>
        public static IMsgUtility PRTxLogReqMsgUtility = ctx["icash2TOLReqMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定購貨取消TxLog的Response電文轉換用的物件(0641)
        /// </summary>
        public static IMsgUtility PRTxLogRespMsgUtility = ctx["icash2TOLRespMsgUtility"] as IMsgUtility;
        #endregion
    }
}
