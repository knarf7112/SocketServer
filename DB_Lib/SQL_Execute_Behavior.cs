using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//
using System.Data;

namespace DB_Lib
{
    /// <summary>
    /// SQL execute behavior object
    /// </summary>
    public abstract class SQL_Execute_Behavior
    {
        /// <summary>
        /// 委派給外部執行的Exec_NonQuery方法
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public delegate int Exec_NonQuery(IDbCommand cmd,IDbDataParameter[] paras);
        /// <summary>
        /// 執行多個Exec_NonQuery並在最外層包Transaction,若有其一失敗則Rollback
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="dele_method"></param>
        /// <param name="paras"></param>
        /// <param name="cmds"></param>
        public void Transaction_Multi_Exec_NonQuery(IDbConnection conn, Exec_NonQuery dele_method, IDbDataParameter[][] paras, params IDbCommand[] cmds)
        {
            IDbTransaction trans = null;
            try
            {
                trans = conn.BeginTransaction();
                for (int i = 0; i < cmds.Length;i++)
                {
                    if (dele_method != null)
                    {
                        dele_method.Invoke(cmds[i], paras[i]);
                    }
                }
                trans.Commit();
            }
            catch (Exception ex)
            {
                trans.Rollback();
                throw ex;
            }
        }

        /// <summary>
        /// 若有參數則帶入sqlCmd,執行ExecuteNonQuery
        /// </summary>
        /// <param name="cmd">sql command</param>
        /// <param name="paras">sql parameters</param>
        /// <returns>變更値</returns>
        public virtual int Execute_NonQuery(IDbCommand cmd, IDbDataParameter[] paras = null)
        {
            this.Set_Parameters(cmd, paras);
            return cmd.ExecuteNonQuery();
        }
        /// <summary>
        /// 執行Execute_Scalar
        /// </summary>
        /// <typeparam name="T">回傳的結果型態</typeparam>
        /// <param name="cmd"></param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public virtual T Execute_Scalar<T>(IDbCommand cmd, IDbDataParameter[] paras = null) where T : class
        {
            this.Set_Parameters(cmd, paras);
            T obj = cmd.ExecuteScalar() as T;
            return obj;
        }

        /// <summary>
        /// 執行sqlcommand的ExecuteReader並讓外部帶入委派方法來轉成自訂型別物件
        /// </summary>
        /// <typeparam name="T">外部所要轉的物件類別</typeparam>
        /// <param name="cmd"></param>
        /// <param name="parsePOCO_method">轉物件用的委派方法</param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public virtual IList<T> Execute_ReaderToPOCO<T>(IDbCommand cmd, ParseToPOCO<T> parsePOCO_method, IDbDataParameter[] paras = null)
        {

            IList<T> list = null;
            this.Set_Parameters(cmd, paras);
            using (IDataReader reader = cmd.ExecuteReader())
            {
                list = RowsMapping<T>(reader, parsePOCO_method);
            }
            return list;
        }
        /// <summary>
        /// 設定sqlCommand的dataParameters,無則跳過
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="paras"></param>
        public virtual void Set_Parameters(IDbCommand cmd, IDbDataParameter[] paras)
        {
            if (paras != null)
            {
                foreach (IDbDataParameter para in paras)
                {
                    cmd.Parameters.Add(para);
                }
            }
        }
        #region Delegate dataReader Mapping parse POCO
        /// <summary>
        /// 從DataReader取得數據轉型成指定型別並out出來,若沒數據可讀則回傳Null
        /// </summary>
        /// <param name="dataReader"></param>
        /// <param name="obj">輸出物件</param>
        /// <typeparam name="T">要轉換的POCO型別</typeparam>
        /// <returns>True:Reader有讀取到數據/False:Reader沒有讀取到任何數據</returns>
        public delegate bool ParseToPOCO<T>(IDataReader dataReader, out T obj);
        /// <summary>
        /// 回傳Mapping後的物件列表
        /// </summary>
        /// <param name="dataReader"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public virtual IList<T> RowsMapping<T>(IDataReader dataReader, ParseToPOCO<T> method)
        {
            if (method == null)
                throw new Exception("無指定轉型的指派方法可呼叫");
            bool hasData = false;
            IList<T> list = new List<T>();
            do
            {
                T obj = default(T);
                if (hasData = method.Invoke(dataReader, out obj))
                {
                    list.Add(obj);
                }
            }
            while (hasData);
            if (list.Count == 0)
            {
                return null;
            }
            else
            {
                return list;
            }
        }
        #endregion
    }
}
