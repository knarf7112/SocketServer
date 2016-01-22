using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using Common.Logging;
//
using Spring;
using Spring.Context;
using Spring.Context.Support;
//
using Kms2.Crypto;
using Kms2.Crypto.Common;
using Kms2.Crypto.Utility;


namespace SocketServer.v2.Handlers
{
    /// <summary>
    /// 用來算MAC用所需的靜態成員
    /// </summary>
    public class CheckMacContainer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(CheckMacContainer));

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
        /// MsgUtility Object : 截取自動加值的Request電文欄位用的物件
        /// </summary>
        public static IMsgUtility AOLReqMsgUtility = ctx["icash2AOLReqMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定自動加值的Response電文欄位用的物件
        /// </summary>
        public static IMsgUtility AOLRespMsgUtility = ctx["icash2AOLRespMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定自動加值TxLog的Request電文欄位用的物件
        /// </summary>
        public static IMsgUtility TOLReqMsgUtility = ctx["icash2TOLReqMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定自動加值TxLog的Response電文欄位用的物件
        /// </summary>
        public static IMsgUtility TOLRespMsgUtility = ctx["icash2TOLRespMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 截取自動加值查詢的Request電文欄位用的物件
        /// </summary>
        public static IMsgUtility ALQReqMsgUtility = ctx["icash2ALQReqMsgUtility"] as IMsgUtility;
        /// <summary>
        /// MsgUtility Object : 設定自動加值查詢的Response電文欄位用的物件
        /// </summary>
        public static IMsgUtility ALQRespMsgUtility = ctx["icash2ALQRespMsgUtility"] as IMsgUtility;
        #endregion

    }
}
