using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//
using System.Data;

namespace DB_Lib
{
    /// <summary>
    /// 資料庫數據轉指定型別物件列表
    /// IDataReader Mapping POCO List Handler
    /// </summary>
    public class RowMapper
    {
        /// <summary>
        /// 從DataReader取得數據轉型成指定型別並out出來,若沒數據可讀則回傳Null
        /// </summary>
        /// <param name="dataReader"></param>
        /// <param name="obj">輸出物件</param>
        /// <typeparam name="T">要轉換的POCO型別</typeparam>
        /// <returns>True:Reader有讀取到數據/False:Reader沒有讀取到任何數據</returns>
        public delegate bool ParseToPOCO<T>(IDataReader dataReader,out T obj);
        /// <summary>
        /// 回傳Mapping後的物件列表
        /// </summary>
        /// <param name="dataReader"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public IList<T> RowsMapping<T>(IDataReader dataReader,ParseToPOCO<T> method)
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
            if(list.Count > 0)
            {
                return null;
            }
            else
            {
                return list;
            } 
        }
    }
}
