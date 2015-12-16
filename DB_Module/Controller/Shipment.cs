using System;
//db operate
using ALCommon;
using System.Data.SqlClient;
using System.Data;
using DB_Module.SQL;
using System.Diagnostics;
//poco
using System.Transactions;

namespace DB_Module.Controller
{
    /// <summary>
    /// 卡機出貨DB操作
    /// </summary>
    public class Shipment
    {
        private static Sql_Getter_SAM_D _Sql_Getter_SAM_D;

        private static Sql_Getter_Reader_D _Sql_Getter_Reader_D;

        public bool IsDebugWriteLine { set; get; }

        /// <summary>
        /// get (table:SAM_D) sql cmd and sqlParameter array to operate
        /// </summary>
        protected Sql_Getter_SAM_D Operate_SAM_D 
        {
            get
            {
                if (_Sql_Getter_SAM_D == null)
                {
                    _Sql_Getter_SAM_D = new Sql_Getter_SAM_D();
                }
                return _Sql_Getter_SAM_D;
            }
        }
        /// <summary>
        /// get (table:Reader_D) sql cmd and sqlParameter array to operate
        /// </summary>
        protected Sql_Getter_Reader_D Operate_Reader_D
        {
            get
            {
                if (_Sql_Getter_Reader_D == null)
                {
                    _Sql_Getter_Reader_D = new Sql_Getter_Reader_D();
                }
                return _Sql_Getter_Reader_D;
            }
        }

        /// <summary>
        /// 依據卡機UID和M_Kind檢查是否存在於SAM主檔
        /// </summary>
        /// <param name="obDB">DB連線模組</param>
        /// <param name="uid">卡機UID(7 bytes=>14 hex string)</param>
        /// <param name="m_kind">卡片代別(default:21)</param>
        /// <returns>true:存在/false:不存在</returns>
        public bool Check_ReaderId_FromSAM_D(AL_DBModule obDB,string uid,string m_kind)
        {
            try
            {
                //判斷資料庫連線有無開啟            
                if (obDB != null && obDB.ALCon.State != ConnectionState.Open)
                {
                    obDB.ALCon.Open();
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //SqlMod_Chk_Bkl BkL = new SqlMod_Chk_Bkl();//產生sql指令
            //Sql_Getter_SAM_D operate_SAM_D = new Sql_Getter_SAM_D();
            SqlParameter[] SqlParamSet = null;
            String sSql; //查詢SQL
            
            try
            {
                m_kind = String.IsNullOrEmpty(m_kind) ? "21" : m_kind;
                //檢查ReaderId('86' + uid)是否存在於SAM主檔
                //operate_SAM_D.Exist_ReaderId(uid, out sSql, out SqlParamSet, m_kind);
                this.Operate_SAM_D.Exist_OSN(uid, out sSql, out SqlParamSet, m_kind);

                string result = obDB.SqlExecuteScalarHasParams(sSql, SqlParamSet);
                return (result == "0" || result == "") ? false : true;

            }
            catch (SqlException sqlEx)
            {
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("[Shipment][Check_ReaderId_FromSAM_D]Sql Error:" + sqlEx.Message);
                }
                throw sqlEx;
            }
            catch (Exception ex)
            {
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("[Shipment][Check_ReaderId_FromSAM_D]Error:" + ex.Message);
                }
                throw ex;
            }
            finally
            {
                //operate_SAM_D = null;
                Array.Resize(ref SqlParamSet, 0);
            }
        }

        /// <summary>
        /// Reader出貨流程
        /// 1.檢查ReaderId是否存在於Reader主檔
        /// 2.更新或新增數據至Reader主檔
        /// 3.根據原來的SAM UID將SAM表格資料備份到SAMDIFF
        /// 4.刪除SAM表格原來UID的數據
        /// 5.重新新增此UID的資料到SAM表格
        /// </summary>
        /// <param name="obDB">DB模組</param>
        /// <param name="uid">SAM UID(OSN)14 hex string</param>
        /// <param name="deviceId">卡機DeviceId:32 hex string</param>
        public void Shipment_Reader(AL_DBModule obDB,string uid, string deviceId)
        {

            string readerId = (Sql_Getter_Reader_D.Reader_Head + uid);//'86' + osn
            bool flag = false;
            //1.檢查ReaderId是否存在於Reader主檔
            if (this.Check_ReaderId_FromReader_D(obDB, readerId))
            {
                //更新Reader主檔
                flag  = this.Update_FromReader_D(obDB, uid, deviceId);
            }
            else
            {
                //新增Reader主檔
                flag = this.Insert_FromReader_D(obDB, uid, deviceId);
            }
            if (flag)
            {
                TransactionOptions option = new TransactionOptions();
                option.IsolationLevel = System.Transactions.IsolationLevel.RepeatableRead;
                option.Timeout = new TimeSpan(0, 30, 10);//10sec
                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required,option))
                {
                    //1.依據UID將SAM_D表格資料備份到SAMDIFF_D表格
                    this.Copy_SAM_D_To_SADDIFF(obDB, uid);
                    //2.依據UID刪除SAM_D資料
                    this.Delete_SAM_D(obDB, uid);
                    //3.依據UID新增一筆資料到SAM_D
                    this.Insert_SAM_D(obDB, uid);
                    scope.Complete();
                }
            }
        }

        #region Protected Method
        /// <summary>
        /// 依據卡機ReaderId檢查是否存在於Reader主檔
        /// </summary>
        /// <param name="obDB">DB連線模組</param>
        /// <param name="readerId">卡機ReaderId(14 hex string)</param>
        /// <returns>true:存在/false:不存在</returns>
        protected bool Check_ReaderId_FromReader_D(AL_DBModule obDB, string readerId)
        {
            try
            {
                //判斷資料庫連線有無開啟            
                if (obDB != null && obDB.ALCon.State != ConnectionState.Open)
                {
                    obDB.ALCon.Open();
                }
            }
            catch (SqlException ex)
            {
                Debug.WriteLine(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //Sql_Getter_Reader_D operate_Reader_D = new Sql_Getter_Reader_D();//產生sql指令
            SqlParameter[] SqlParamSet = null;
            String sSql; //查詢SQL

            try
            {
                //檢查ReaderId('86' + uid)是否存在於SAM主檔
                //operate_Reader_D.Exist_ReaderId(readerId, out sSql, out SqlParamSet);
                this.Operate_Reader_D.Exist_ReaderId(readerId, out sSql, out SqlParamSet);
                string result = obDB.SqlExecuteScalarHasParams(sSql, SqlParamSet);
                return (result == "0" || result == "") ? false : true;

            }
            catch (SqlException sqlEx)
            {
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("[Shipment][Check_ReaderId_FromReader_D]Sql Error:" + sqlEx.Message);
                }
                throw sqlEx;
            }
            catch (Exception ex)
            {
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("[Shipment][Check_ReaderId_FromReader_D]Error:" + ex.Message);
                }
                throw ex;
            }
            finally
            {
                //operate_Reader_D = null;
                Array.Resize(ref SqlParamSet, 0);
            }
        }
        /// <summary>
        /// 依據ReaderId 更新Reader主檔
        /// 資料物件必須有資料的欄位(READER_ID,READER_TYPE,READER_STS,TERMINAL_PSN,SAM_ID,SH_DATE,UPT_DATE,UPT_TIME,READER_ID)
        /// 取得更新Reader主檔的SQL參數和命令
        /// </summary>
        /// <param name="obDB">DB連線模組</param>
        /// <param name="uid">SAM uid(14 hex string)</param>
        /// <param name="deviceId">deviceId(16 bytes)</param>
        /// <returns>true:更新成功/false:更新後數據無變動</returns>
        protected bool Update_FromReader_D(AL_DBModule obDB, string uid, string deviceId)
        {
            try
            {
                //判斷資料庫連線有無開啟            
                if (obDB != null && obDB.ALCon.State != ConnectionState.Open)
                {
                    obDB.ALCon.Open();
                }
            }
            catch (SqlException ex)
            {
                Debug.WriteLine(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //Sql_Getter_Reader_D operate_Reader_D = new Sql_Getter_Reader_D();//產生sql指令
            SqlParameter[] SqlParamSet = null;
            String sSql; //查詢SQL

            try
            {
                //依據ReaderId('86' + uid)更新Reader主檔
                //operate_Reader_D.Update(uid, deviceId, out sSql, out SqlParamSet);
                this.Operate_Reader_D.Update(uid, deviceId, out sSql, out SqlParamSet);

                string result = obDB.SqlExec1(sSql, SqlParamSet);
                //if (IsDebugWriteLine)
                //{
                //    Debug.WriteLine("Result:" + ((result == "1") ? "Success" : "NoneChange"));
                //}
                return (result == "1") ? true : false;
            }
            catch (SqlException sqlEx)
            {
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("[Shipment][Update_FromReader_D]Sql Error:" + sqlEx.Message);
                }
                throw sqlEx;
            }
            catch (Exception ex)
            {
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("[Shipment][Update_FromReader_D]Error:" + ex.Message);
                }
                throw ex;
            }
            finally
            {
                //operate_Reader_D = null;
                Array.Resize(ref SqlParamSet, 0);
            }
        }
        /// <summary>
        /// 依據ReaderId 新增Reader主檔
        /// SAM uid(14 hex string), deviceId(16 bytes)
        /// 取得更新Reader主檔的SQL參數和命令
        /// </summary>
        /// <param name="obDB">DB連線模組</param>
        /// <param name="uid">SAM uid(14 hex string)</param>
        /// <param name="deviceId">deviceId(16 bytes)</param>
        /// <returns>true:新增成功/false:新增後數據無變動</returns>
        protected bool Insert_FromReader_D(AL_DBModule obDB, string uid, string deviceId)
        {
            try
            {
                //判斷資料庫連線有無開啟            
                if (obDB != null && obDB.ALCon.State != ConnectionState.Open)
                {
                    obDB.ALCon.Open();
                }
            }
            catch (SqlException ex)
            {
                Debug.WriteLine(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //Sql_Getter_Reader_D operate_Reader_D = new Sql_Getter_Reader_D();//產生sql指令
            SqlParameter[] SqlParamSet = null;
            String sSql; //查詢SQL

            try
            {
                //依據ReaderId('86' + uid)新增Reader主檔
                //operate_Reader_D.Insert(uid, deviceId, out sSql, out SqlParamSet);
                this.Operate_Reader_D.Insert(uid, deviceId, out sSql, out SqlParamSet);

                string result = obDB.SqlExec1(sSql, SqlParamSet);
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("Result:" + ((result == "1") ? "Success" : "NoneChange"));
                }
                return (result == "1") ? true : false;
            }
            catch (SqlException sqlEx)
            {
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("[Shipment][Insert_FromReader_D]Sql Error:" + sqlEx.Message);
                }
                throw sqlEx;
            }
            catch (Exception ex)
            {
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("[Shipment][Insert_FromReader_D]Error:" + ex.Message);
                }
                throw ex;
            }
            finally
            {
                //operate_Reader_D = null;
                Array.Resize(ref SqlParamSet, 0);
            }
        }

        /// <summary>
        /// 依據uid(osn)將SAM主檔的Row data備份到SAMDIFF_D表格
        /// </summary>
        /// <param name="obDB"></param>
        /// <param name="uid">SAM UID(14 hex string)</param>
        /// <returns>true:新增成功/false:新增數據無變動</returns>
        protected bool Copy_SAM_D_To_SADDIFF(AL_DBModule obDB, string uid)
        {
            try
            {
                //判斷資料庫連線有無開啟            
                if (obDB != null && obDB.ALCon.State != ConnectionState.Open)
                {
                    obDB.ALCon.Open();
                }
            }
            catch (SqlException ex)
            {
                Debug.WriteLine(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //SqlMod_Chk_Bkl BkL = new SqlMod_Chk_Bkl();//產生sql指令
            //Sql_Getter_SAM_D operate_SAM_D = new Sql_Getter_SAM_D();
            SqlParameter[] SqlParamSet = null;
            String sSql; //查詢SQL

            try
            {
                //依據uid(osn)將SAM主檔的Row data備份到SAMDIFF_D表格
                //operate_SAM_D.Insert_To_SAMDIFF_D(uid, out sSql, out SqlParamSet,"21");
                this.Operate_SAM_D.Insert_To_SAMDIFF_D(uid, out sSql, out SqlParamSet, "21");

                string result = obDB.SqlExec1(sSql, SqlParamSet);
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("Result:" + ((result == "1") ? "Success" : "NoneChange"));
                }
                return (result == "1") ? true : false;
            }
            catch (SqlException sqlEx)
            {
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("[Shipment][Copy_SAM_D_To_SADDIFF]Sql Error:" + sqlEx.Message);
                }
                throw sqlEx;
            }
            catch (Exception ex)
            {
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("[Shipment][Copy_SAM_D_To_SADDIFF]Error:" + ex.Message);
                }
                throw ex;
            }
            finally
            {
                //operate_SAM_D = null;
                Array.Resize(ref SqlParamSet, 0);
            }
        }
        /// <summary>
        /// 依據OSN來刪除SAM主檔一筆Row data
        /// </summary>
        /// <param name="obDB">DB連線模組</param>
        /// <param name="uid">SAM UID</param>
        protected void Delete_SAM_D(AL_DBModule obDB, string uid)
        {
            try
            {
                //判斷資料庫連線有無開啟            
                if (obDB != null && obDB.ALCon.State != ConnectionState.Open)
                {
                    obDB.ALCon.Open();
                }
            }
            catch (SqlException ex)
            {
                Debug.WriteLine(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //Sql_Getter_SAM_D operate_SAM_D = new Sql_Getter_SAM_D();//產生sql指令
            SqlParameter[] SqlParamSet = null;
            String sSql; //查詢SQL

            try
            {
                //依據uid(osn)將SAM主檔的Row data刪除
                //operate_SAM_D.Delete(uid, out sSql, out SqlParamSet);
                this.Operate_SAM_D.Delete(uid, out sSql, out SqlParamSet);

                string result = obDB.SqlExec1(sSql, SqlParamSet);
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("Result:" + ((result == "1") ? "Success" : "NoneChange"));
                }
                //return (result == "1") ? true : false;
            }
            catch (SqlException sqlEx)
            {
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("[Shipment][Delete_SAM_D]Sql Error:" + sqlEx.Message);
                }
                throw sqlEx;
            }
            catch (Exception ex)
            {
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("[Shipment][Delete_SAM_D]Error:" + ex.Message);
                }
                throw ex;
            }
            finally
            {
                //operate_SAM_D = null;
                Array.Resize(ref SqlParamSet, 0);
            }
        }

        /// <summary>
        /// 依據uid來新增一筆資料到SAM_D表格
        /// </summary>
        /// <param name="obDB">DB模組</param>
        /// <param name="uid">SAM UID(OSN)7 bytes</param>
        protected void Insert_SAM_D(AL_DBModule obDB, string uid)
        {
            try
            {
                //判斷資料庫連線有無開啟            
                if (obDB != null && obDB.ALCon.State != ConnectionState.Open)
                {
                    obDB.ALCon.Open();
                }
            }
            catch (SqlException ex)
            {
                Debug.WriteLine(ex);
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //Sql_Getter_SAM_D operate_SAM_D = new Sql_Getter_SAM_D();//產生sql指令
            SqlParameter[] SqlParamSet = null;
            String sSql; //查詢SQL

            try
            {
                //依據uid(osn)將SAM主檔的Row data刪除
                //operate_SAM_D.Insert(uid, out sSql, out SqlParamSet);
                this.Operate_SAM_D.Insert(uid, out sSql, out SqlParamSet);
                string result = obDB.SqlExec1(sSql, SqlParamSet);
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("Result:" + ((result == "1") ? "Success" : "NoneChange"));
                }
                //return (result == "1") ? true : false;
            }
            catch (SqlException sqlEx)
            {
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("[Shipment][Insert_SAM_D]Sql Error:" + sqlEx.Message);
                }
                throw sqlEx;
            }
            catch (Exception ex)
            {
                if (IsDebugWriteLine)
                {
                    Debug.WriteLine("[Shipment][Insert_SAM_D]Error:" + ex.Message);
                }
                throw ex;
            }
            finally
            {
                //operate_SAM_D = null;
                Array.Resize(ref SqlParamSet, 0);
            }
        }
        #endregion
    }
}
