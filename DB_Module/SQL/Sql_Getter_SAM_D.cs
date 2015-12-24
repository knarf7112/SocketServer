using System;
//
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace DB_Module.SQL
{
    /// <summary>
    /// 卡機出貨SQL
    /// </summary>
    public partial class Sql_Getter_SAM_D : Sql_Base
    {
        public static readonly string READER_Head = "86";

        public static readonly string DIFF_FLG = "92";

        public static readonly string SAM_TYPE = "RW";

        public static readonly string M_KIND = "21";
        /// <summary>
        /// SAM主檔表格名稱
        /// </summary>
        public string tableName { get; set; }
        /// <summary>
        /// 備份SAM主檔的表格
        /// </summary>
        public string tableName2 { get; set; }
        public Sql_Getter_SAM_D()
        {
            //SAM主檔
            this.tableName = "dbo.TM_ISET_SAM_D";
            //backup table -- 備份SAM主檔用的Table
            this.tableName2 = "dbo.TM_ISET_SAMDIFF_D";
        }

        /// <summary>
        /// 依據卡機UID和M_Kind檢核是否存在於SAM主檔(1:存在/0:不存在)
        /// 取得檢核SAM主檔的SQL參數和命令
        /// </summary>
        /// <param name="osn">卡機OSN(UID:7 bytes)</param>
        /// <param name="cmdText">sql cmd</param>
        /// <param name="sqlParams">sql parameters</param>
        /// <param name="m_Kind">卡片代別(default:21)</param>
        public void Exist_OSN(string osn, out string cmdText, out SqlParameter[] sqlParams, string m_Kind = "21")
        {
            cmdText = @"select count(*) as count from %Table% " +
                "where M_KIND=@M_KIND and OSN=@OSN ";//@OSN

            cmdText = Regex.Replace(cmdText, "%Table%", this.tableName);

            sqlParams = new SqlParameter[2];//Regex.Matches(cmdText,"@[a-zA-Z]").Count];
            for (int i = 0; i < sqlParams.Length; i++)
            {
                sqlParams[i] = new SqlParameter();
                sqlParams[i].Direction = System.Data.ParameterDirection.Input;
                sqlParams[i].DbType = System.Data.DbType.AnsiString;
                //https://msdn.microsoft.com/zh-tw/library/cc716729(v=vs.110).aspx
                //sqlParams[i].SqlDbType = System.Data.SqlDbType.Char;
            }
            sqlParams[0].ParameterName = "@M_KIND";
            sqlParams[0].Value = m_Kind;
            sqlParams[1].ParameterName = "@OSN";
            sqlParams[1].Value = osn;//SAM uid

        }

        public void Insert_To_SAMDIFF_D(string osn, out string cmdText, out SqlParameter[] sqlParams, string m_Kind = "21")
        {

            cmdText = @"INSERT INTO %Table2%
(
LOG_DATETIME, DIFF_FLG, SETTLE_DATE, ACT_DATE, M_KIND, MERCHANT_NO, STORE_NO, OSN, REG_ID, READER_ID, TSM_CID, LPSM_CID, TSM_OID, LSM_OID, PSM_OID, STATUS, TSM_CADID, TSM_TSN, TSM_SBN, TSM_CP, LSM_HOSTCC, LSM_HOSTCA, LSM_HOSTCSN, LSM_HOSTCV, LSM_CSN, LSM_CA, LSM_CAL, LSM_HOSTDC, LSM_HOSTDA, LSM_HOSTDSN, LSM_HOSTDV, LSM_DSN, LSM_DA, PSM_HOSTCC, PSM_HOSTCA, PSM_HOSTCSN, PSM_HOSTCV, PSM_CSN, PSM_CA, PSM_SBN, PSM_NRS, PSM_SV, ICCSALE_HOSTC, ICCSALE_HOSTA, ICCSALE_SEQNO, ICCSALE_VALUE, ICCSALER_HOSTC, ICCSALER_HOSTA, ICCSALER_SEQNO, ICCSALER_VALUE, DEPLOY_DATE, ABORT_DATE, TRANS_DATE_TXLOG
) 
  SELECT @LOG_DATETIME,@DIFF_FLG,@SETTLE_DATE,@ACT_DATE,M_KIND,MERCHANT_NO,STORE_NO,OSN,REG_ID,READER_ID,TSM_CID,LPSM_CID,TSM_OID,LSM_OID
         ,PSM_OID,STATUS,TSM_CADID,TSM_TSN,TSM_SBN,TSM_CP,LSM_HOSTCC,LSM_HOSTCA,LSM_HOSTCSN,LSM_HOSTCV
         ,LSM_CSN,LSM_CA,LSM_CAL,LSM_HOSTDC,LSM_HOSTDA,LSM_HOSTDSN,LSM_HOSTDV,LSM_DSN,LSM_DA,PSM_HOSTCC
         ,PSM_HOSTCA,PSM_HOSTCSN,PSM_HOSTCV,PSM_CSN,PSM_CA,PSM_SBN,PSM_NRS,PSM_SV,ICCSALE_HOSTC,ICCSALE_HOSTA
         ,ICCSALE_SEQNO,ICCSALE_VALUE,ICCSALER_HOSTC,ICCSALER_HOSTA,ICCSALER_SEQNO,ICCSALER_VALUE,DEPLOY_DATE
         ,ABORT_DATE,null
    FROM %Table%
   WHERE OSN = @OSN
";

              cmdText = Regex.Replace(cmdText, "%Table%", this.tableName);
              cmdText = Regex.Replace(cmdText, "%Table2%", this.tableName2);
              sqlParams = new SqlParameter[5];//Regex.Matches(cmdText,"@[a-zA-Z]").Count];
              for (int i = 0; i < sqlParams.Length; i++)
              {
                  sqlParams[i] = new SqlParameter();
                  sqlParams[i].Direction = System.Data.ParameterDirection.Input;
                  sqlParams[i].DbType = System.Data.DbType.AnsiString;
                  //https://msdn.microsoft.com/zh-tw/library/cc716729(v=vs.110).aspx
                  //sqlParams[i].SqlDbType = System.Data.SqlDbType.Char;
              }
              DateTime now = DateTime.Now;
              sqlParams[0].ParameterName = "@LOG_DATETIME";
              sqlParams[0].Value = now.ToString("yyyy/MM/dd HH:mm:ss");
              sqlParams[1].ParameterName = "@SETTLE_DATE";
              sqlParams[1].Value = now.ToString("yyyyMMdd");
              sqlParams[2].ParameterName = "@ACT_DATE";
              sqlParams[2].Value = now.ToString("yyyyMMdd");
              sqlParams[3].ParameterName = "@OSN";
              sqlParams[3].Value = osn;
              sqlParams[4].ParameterName = "@DIFF_FLG";
              sqlParams[4].Value = Sql_Getter_SAM_D.DIFF_FLG;
        }

        /// <summary>
        /// 依據OSN(14 hex string)來刪除SAM主檔Row資料
        /// </summary>
        /// <param name="osn">SAM UID(14 hex string)</param>
        /// <param name="cmdText">sql command</param>
        /// <param name="sqlParams">sql parameter</param>
        public void Delete(string osn, out string cmdText, out SqlParameter[] sqlParams)
        {
            cmdText = @"delete %Table% where OSN=@OSN";

            cmdText = Regex.Replace(cmdText, "%Table%", this.tableName);

            sqlParams = new SqlParameter[1];//Regex.Matches(cmdText,"@[a-zA-Z]").Count];
            for (int i = 0; i < sqlParams.Length; i++)
            {
                sqlParams[i] = new SqlParameter();
                sqlParams[i].Direction = System.Data.ParameterDirection.Input;
                sqlParams[i].DbType = System.Data.DbType.AnsiString;
                //https://msdn.microsoft.com/zh-tw/library/cc716729(v=vs.110).aspx
                //sqlParams[i].SqlDbType = System.Data.SqlDbType.Char;
            }
            sqlParams[0].ParameterName = "@OSN";
            sqlParams[0].Value = osn;
        }

        /// <summary>
        /// 依據OSN新增1筆資料到SAM主檔 
        /// </summary>
        /// <param name="osn">SAM UID(14 hex string)</param>
        /// <param name="cmdText">sql command</param>
        /// <param name="sqlParams">sql parameter</param>
        public void Insert(string osn, out string cmdText, out SqlParameter[] sqlParams)
        {
            cmdText = @"INSERT INTO %Table%(M_KIND, OSN,SAM_TYPE,READER_ID,TSM_CID,LPSM_CID,DEPLOY_DATE) 
                   VALUES(@M_KIND,@OSN,@SAM_TYPE,@READER_ID,@TSM_CID,@LPSM_CID,@DEPLOY_DATE)";

            cmdText = Regex.Replace(cmdText, "%Table%", this.tableName);

            sqlParams = new SqlParameter[7];//Regex.Matches(cmdText,"@[a-zA-Z]").Count];
            for (int i = 0; i < sqlParams.Length; i++)
            {
                sqlParams[i] = new SqlParameter();
                sqlParams[i].Direction = System.Data.ParameterDirection.Input;
                sqlParams[i].DbType = System.Data.DbType.AnsiString;
                //https://msdn.microsoft.com/zh-tw/library/cc716729(v=vs.110).aspx
                //sqlParams[i].SqlDbType = System.Data.SqlDbType.Char;
            }
            sqlParams[0].ParameterName = "@OSN";
            sqlParams[0].Value = osn;
            sqlParams[1].ParameterName = "@SAM_TYPE";
            sqlParams[1].Value = Sql_Getter_SAM_D.SAM_TYPE;
            sqlParams[2].ParameterName = "@READER_ID";
            sqlParams[2].Value = (Sql_Getter_SAM_D.READER_Head + osn);
            sqlParams[3].ParameterName = "@TSM_CID";
            sqlParams[3].Value = osn;
            sqlParams[4].ParameterName = "@LPSM_CID";
            sqlParams[4].Value = osn;
            sqlParams[5].ParameterName = "@DEPLOY_DATE";
            sqlParams[5].Value = DateTime.Now.ToString("yyyyMMdd");
            sqlParams[6].ParameterName = "@M_KIND";
            sqlParams[6].Value = Sql_Getter_SAM_D.M_KIND;
        }
    }
}
