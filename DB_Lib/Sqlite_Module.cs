using System;
using System.Collections.Generic;
using System.Data;
//
using System.Text.RegularExpressions;
//
using System.IO;
//
using System.Data.SQLite;
//DbParameter

namespace DB_Lib
{
    /// <summary>
    /// 使用Sqlite_x86模組
    /// </summary>
    public class Sqlite_Module : SQL_Execute_Behavior,IDatabase_Initial, IDisposable
    {
        private IDbConnection _conn;
        private string _connectionString;
        //放有初始化確認tableName存在與否的集合
        private IDictionary<string, bool> dic_TableChecked;
        /// <summary>
        /// 
        /// </summary>
        public static string DBName_Pattern = @"Data Source=(?'filePath'[A-Za-z0-9/:\\/./_]+.db(?-i));";

        /// <summary>
        /// Database File Exist Flag
        /// </summary>
        public bool DB_FileIsOk { get; private set; }
        /// <summary>
        /// Record Error Message
        /// </summary>
        public Queue<string> ErrorMsg { get; private set; }
        /// <summary>
        /// Record Work Flow Log Message
        /// </summary>
        public Queue<string> FlowMsg { get; private set; }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="connectionString">連線字串</param>
        /// <param name="conn">Connection物件</param>
        public Sqlite_Module(string connectionString,IDbConnection conn)
        {
            this._connectionString = connectionString;
            this._conn = conn;
            this.DB_FileIsOk = Check_Database(connectionString);
            this._conn.ConnectionString = this._connectionString;
            this.ErrorMsg = new Queue<string>();
            this.FlowMsg = new Queue<string>();
            this.dic_TableChecked = new Dictionary<string, bool>();
        }
        /// <summary>
        /// Inject SqliteConnection and  default connection string
        /// </summary>
        public Sqlite_Module()
            : this("Data Source=database.db;", SQLiteFactory.Instance.CreateConnection())
        {

        }
        /// <summary>
        /// check db file from connection string, if not exist then create it
        /// </summary>
        /// <param name="connectionString">connection string</param>
        /// <returns>true:success/false:error</returns>
        public virtual bool Check_Database(string connectionString)
        {
            Regex regEx = new Regex(DBName_Pattern);// 
            if(!regEx.IsMatch(connectionString)){
                throw new ArgumentException("DB路徑無法取得,請確認!");
            }
            string db_filePath = string.Empty;
            //check connection string
            Match match = regEx.Match(connectionString);
            //match group
            string filePath = match.Groups["filePath"].Value;
            //check absulote root
            if (Path.IsPathRooted(filePath))
            {
                db_filePath = filePath;
            }
            else
            {
                db_filePath = AppDomain.CurrentDomain.BaseDirectory + @"\" + Path.GetFileName(filePath);
            }
            return CreateDataBase(db_filePath);
        }
        /// <summary>
        /// 建立實體db檔案
        /// </summary>
        /// <param name="db_fileFullName"></param>
        /// <returns></returns>
        protected virtual bool CreateDataBase(string db_fileFullName)
        {
            if (!File.Exists(db_fileFullName))
            {
                SQLiteConnection.CreateFile(db_fileFullName);
            }
            return true;
        }

        #region Table Operate
        /// <summary>
        /// 建立單一Table
        /// </summary>
        /// <param name="tableName">table name</param>
        /// <param name="table_paras">declare column name</param>
        /// <returns>true:success/false:error</returns>
        public bool Create_Table(string tableName, string table_paras)
        {
            try
            {
                //ref:http://stackoverflow.com/questions/5870284/can-i-use-parameters-for-the-table-name-in-sqlite3
                //SQLite Parameters - Not allowing tablename as parameter
                string sql_ceate_table = String.Format("create table if not exists {0} {1}", tableName.Trim(), table_paras.Trim());//(col1 typ1, ..., colN typN)";
                using (IDbCommand cmd = this._conn.CreateCommand())
                {
                    if (this._conn.State != ConnectionState.Open)
                    {
                        this._conn.Open();
                    }
                    cmd.CommandTimeout = 100;//100ms
                    cmd.CommandText = sql_ceate_table;
                    cmd.CommandType = CommandType.Text;
                    int result = this.Execute_NonQuery(cmd);
                    this.FlowMsg.Enqueue(String.Format("{0}[Create_DataTable] Result: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), result.ToString()));

                    return this.dic_TableChecked[tableName] = true;
                }
            }
            catch (SQLiteException ex)
            {
                this.ErrorMsg.Enqueue(String.Format("{0}[Create_DataTable]Error:{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), ex.Message));
                return this.dic_TableChecked[tableName] = false;
            }
        }

        /// <summary>
        /// 建立多個Table
        /// </summary>
        /// <param name="tables_and_paras"></param>
        public void Create_Tables(IDictionary<string,string> tables_and_paras)
        {
            foreach (string tableName in tables_and_paras.Keys)
            {
                if (!String.IsNullOrEmpty(tables_and_paras[tableName]))
                {
                    this.dic_TableChecked[tableName] = this.Create_Table(tableName, tables_and_paras[tableName]);
                }
            }
        }

        /// <summary>
        /// 刪除指定table
        /// </summary>
        /// <param name="tableName">要刪除的table name</param>
        /// <returns></returns>
        public bool Delete_Table(string tableName)
        {

            string sql_delete_table = String.Format("drop table if exists {0}", tableName);
            try
            {
                using (IDbCommand cmd = this._conn.CreateCommand())
                {
                    cmd.CommandText = sql_delete_table;
                    cmd.CommandTimeout = 100;
                    cmd.CommandType = CommandType.Text;
                    
                    int result = this.Execute_NonQuery(cmd);
                    this.FlowMsg.Enqueue(String.Format("{0}[Delete_Table] 完成 {1} Table刪除作業Result: {2}", 
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), tableName, result.ToString()));
                }

                this.dic_TableChecked.Remove(tableName);
                return true;
            }
            catch (SQLiteException ex)
            {
                this.ErrorMsg.Enqueue(String.Format("{0}[Delete_Table]Error:{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), ex.Message));
                return false;
            }
        }
        #endregion

        /// <summary>
        /// 取得新的Sql Command,若無開啟連線則開啟連線
        /// </summary>
        /// <returns>新的Sql Command</returns>
        public IDbCommand Get_New_SQLCmd()
        {
            if (this._conn != null)
            {
                if (this._conn.State != ConnectionState.Open)
                    this._conn.Open();
                return this._conn.CreateCommand();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 沒輸入參數:使用內部設定Connection
        /// 有輸入參數:使用輸入的Connection設定IsolationLevel
        /// </summary>
        /// <param name="conn">connection</param>
        /// <param name="level">isolationlevel</param>
        /// <returns>transaction</returns>
        public IDbTransaction BeginTransaction(IDbConnection conn = null,IsolationLevel level = IsolationLevel.Serializable)
        {
            if (conn == null)
            {
                return this._conn.BeginTransaction();
            }
            else
            {
                return conn.BeginTransaction(level);
            }
        }

        /// <summary>
        /// 關閉連線
        /// </summary>
        public void Close_Connection()
        {
            if (this._conn.State != ConnectionState.Closed)
                this._conn.Close();
        }

        public void Dispose()
        {
            this.ErrorMsg.Clear();
            this.FlowMsg.Clear();
            if (this._conn != null)
            {
                this.Close_Connection();
                this._conn.Dispose();
            }
        }
    }
}
