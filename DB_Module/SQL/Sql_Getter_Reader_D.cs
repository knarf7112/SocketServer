using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
//

namespace DB_Module.SQL
{
    public partial class Sql_Getter_Reader_D : Sql_Base
    {
        //reader type
        public static readonly string READER_TYPE = "03";
        //reader status
        public static readonly string READER_STS = "SH";

        public static readonly string Reader_Head = "86";
        //卡片代別
        public static readonly string M_KIND = "21";
        //出貨機構的統一編號:iCash 
        public static readonly string MERCHANT_NO = "54650114";
        //出貨的機構:iCash 
        public static readonly string MERC_FLG = "ICA";
        /// <summary>
        /// table name
        /// </summary>
        public string tableName { get; set; }

        public Sql_Getter_Reader_D()
        {
            this.tableName = "dbo.TM_ISET_READER_D";
        }

        /// <summary>
        /// 依據卡機ReaderId檢核是否存在於Reader主檔(1:存在/0:不存在)
        /// 取得檢核Reader主檔的SQL參數和命令
        /// </summary>
        /// <param name="readerId"></param>
        /// <param name="cmdText"></param>
        /// <param name="sqlParams"></param>
        public void Exist_ReaderId(string readerId,out string cmdText,out SqlParameter[] sqlParams)
        {
            cmdText = @"select count(*) as count from %Table% " +
                      @"where READER_ID=@READER_ID";

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
            sqlParams[0].ParameterName = "@READER_ID";
            sqlParams[0].Value = readerId;

        }
        /// <summary>
        /// 依據ReaderId 更新Reader主檔
        /// 資料物件必須有資料的欄位(READER_ID,READER_TYPE,READER_STS,TERMINAL_PSN,SAM_ID,SH_DATE,UPT_DATE,UPT_TIME,READER_ID)
        /// 取得更新Reader主檔的SQL參數和命令
        /// </summary>
        /// <param name="poco">Reader資料物件</param>
        /// <param name="cmdText">sql command</param>
        /// <param name="sqlParams">sql parameter</param>
        public void Update(string osn, string deviceId, out string cmdText, out SqlParameter[] sqlParams, string reader_Type = null, string merc_flg = null)
        {
            cmdText = @"update %Table% " +
                      @"set MERCHANT_NO=@MERCHANT_NO, READER_TYPE=@READER_TYPE, READER_STS=@READER_STS, " +
                          @"TERMINAL_PSN=@TERMINAL_PSN, SAM_ID=@SAM_ID, MERC_FLG=@MERC_FLG, " +
                          @"SH_DATE=@SH_DATE, UPT_DATE=@UPT_DATE, UPT_TIME=@UPT_TIME " +
                      @"where READER_ID=@READER_ID";

            cmdText = Regex.Replace(cmdText, "%Table%", this.tableName);

            sqlParams = new SqlParameter[10];//Regex.Matches(cmdText,"@[a-zA-Z]").Count];
            for (int i = 0; i < sqlParams.Length; i++)
            {
                sqlParams[i] = new SqlParameter();
                sqlParams[i].Direction = System.Data.ParameterDirection.Input;
                sqlParams[i].DbType = System.Data.DbType.AnsiString;
                //https://msdn.microsoft.com/zh-tw/library/cc716729(v=vs.110).aspx
                //sqlParams[i].SqlDbType = System.Data.SqlDbType.Char;
            }
            DateTime now = DateTime.Now;
            sqlParams[0].ParameterName = "@READER_TYPE";
            sqlParams[0].Value = ((String.IsNullOrEmpty(reader_Type)) || (reader_Type.Length != 2)) ? Sql_Getter_Reader_D.READER_TYPE : reader_Type;
            sqlParams[1].ParameterName = "@READER_STS";
            sqlParams[1].Value = Sql_Getter_Reader_D.READER_STS;
            sqlParams[2].ParameterName = "@TERMINAL_PSN";
            sqlParams[2].Value = deviceId;
            sqlParams[3].ParameterName = "@SAM_ID";
            sqlParams[3].Value = osn;
            sqlParams[4].ParameterName = "@SH_DATE";
            sqlParams[4].Value = now.ToString("yyyyMMddHHmmss");
            sqlParams[5].ParameterName = "@UPT_DATE";
            sqlParams[5].Value = now.ToString("yyyyMMdd");
            sqlParams[6].ParameterName = "@UPT_TIME";
            sqlParams[6].Value = now.ToString("yyyyMMddHHmmss");
            sqlParams[7].ParameterName = "@READER_ID";
            sqlParams[7].Value = Sql_Getter_Reader_D.Reader_Head + osn;//'86' + 14 hex string
            sqlParams[8].ParameterName = "@MERCHANT_NO";
            sqlParams[8].Value = Sql_Getter_Reader_D.MERCHANT_NO;
            sqlParams[9].ParameterName = "@MERC_FLG";
            sqlParams[9].Value = ((String.IsNullOrEmpty(merc_flg)) || (merc_flg.Length != 3)) ? Sql_Getter_Reader_D.MERC_FLG : merc_flg;
        }

        /// <summary>
        /// 依據ReaderId新增資料
        /// 資料物件必須有資料的欄位(READER_ID,M_KIND,READER_TYPE,READER_STS,TERMINAL_PSN,SAM_ID,SH_DATE,UPT_DATE,UPT_TIME,READER_ID)
        /// 到Reader主檔
        /// 取得新增Reader主檔的SQL參數和命令
        /// </summary>
        /// <param name="poco"></param>
        /// <param name="cmdText"></param>
        /// <param name="sqlParams"></param>
        public void Insert(string osn,string deviceId, out string cmdText, out SqlParameter[] sqlParams,string reader_Type = null,string merc_flg = null)
        {
            cmdText = @"insert into %Table% " +
                      @"( READER_ID, M_KIND, MERCHANT_NO, READER_TYPE, READER_STS, TERMINAL_PSN, SAM_ID, MERC_FLG, " +
                       @"SH_DATE, UPT_DATE, UPT_TIME ) " +
                      @"values( @READER_ID, @M_KIND, @MERCHANT_NO, @READER_TYPE, @READER_STS, @TERMINAL_PSN, @SAM_ID, @MERC_FLG, " +
                       @"@SH_DATE, @UPT_DATE, @UPT_TIME)";

            cmdText = Regex.Replace(cmdText, "%Table%", this.tableName);

            sqlParams = new SqlParameter[11];//Regex.Matches(cmdText,"@[a-zA-Z]").Count];
            for (int i = 0; i < sqlParams.Length; i++)
            {
                sqlParams[i] = new SqlParameter();
                sqlParams[i].Direction = System.Data.ParameterDirection.Input;
                sqlParams[i].DbType = System.Data.DbType.AnsiString;
                //https://msdn.microsoft.com/zh-tw/library/cc716729(v=vs.110).aspx
                //sqlParams[i].SqlDbType = System.Data.SqlDbType.Char;
            }
            DateTime now = DateTime.Now;
            sqlParams[0].ParameterName = "@READER_TYPE";
            sqlParams[0].Value = ((String.IsNullOrEmpty(reader_Type)) || (reader_Type.Length != 2)) ? Sql_Getter_Reader_D.READER_TYPE : reader_Type;//--00:沒卡機、01:NEC R6 卡機、02:MPG/CRF 卡機、03:CASTLES/V5s 卡機
            sqlParams[1].ParameterName = "@READER_STS";
            sqlParams[1].Value = Sql_Getter_Reader_D.READER_STS;
            sqlParams[2].ParameterName = "@TERMINAL_PSN";
            sqlParams[2].Value = deviceId;
            sqlParams[3].ParameterName = "@SAM_ID";
            sqlParams[3].Value = osn;
            sqlParams[4].ParameterName = "@SH_DATE";
            sqlParams[4].Value = now.ToString("yyyyMMddHHmmss");
            sqlParams[5].ParameterName = "@UPT_DATE";
            sqlParams[5].Value = now.ToString("yyyyMMdd");
            sqlParams[6].ParameterName = "@UPT_TIME";
            sqlParams[6].Value = now.ToString("yyyyMMddHHmmss");
            sqlParams[7].ParameterName = "@READER_ID";
            sqlParams[7].Value = Sql_Getter_Reader_D.Reader_Head + osn;
            sqlParams[8].ParameterName = "@M_KIND";
            sqlParams[8].Value = Sql_Getter_Reader_D.M_KIND;
            sqlParams[9].ParameterName = "@MERCHANT_NO";
            sqlParams[9].Value = Sql_Getter_Reader_D.MERCHANT_NO;
            sqlParams[10].ParameterName = "@MERC_FLG";
            sqlParams[10].Value = ((String.IsNullOrEmpty(merc_flg)) || (merc_flg.Length != 3)) ? Sql_Getter_Reader_D.MERC_FLG : merc_flg;
        }
    }
}
