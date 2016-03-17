using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DB_Lib
{
    public interface IDatabase_Initial
    {
        /// <summary>
        /// check database if not exist then create a new one;
        /// </summary>
        /// <param name="connectionString">connection String</param>
        /// <returns>True:db存在或成功建立新的/異常</returns>
        bool Check_Database(string connectionString);

        /// <summary>
        /// check dataTable; if not exist,ceate it
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="table_paras"></param>
        /// <returns>True:datatable存在或成功建立新的/異常</returns>
        bool Create_Table(string tableName, string table_paras);
        /// <summary>
        /// check multi dataTable ; if not exist,ceate it
        /// </summary>
        /// <param name="tableName">表格名稱</param>
        /// <param name="table_paras">表格的定義欄位字串</param>
        /// <returns>True:datatable存在或成功建立新的/異常</returns>
        void Create_Tables(IDictionary<string, string> tables_and_paras);
    }
}
