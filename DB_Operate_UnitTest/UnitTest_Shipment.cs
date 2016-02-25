using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
//
using DB_Module.Controller;
using ALCommon;
using System.Diagnostics;

namespace DB_Operate_UnitTest
{
    [TestClass]
    public class UnitTest_Shipment
    {
        private Shipment shipment;
        private AL_DBModule obDb; 

        [TestInitialize]
        public void Init()
        {
            this.shipment = new Shipment();
            this.shipment.IsDebugWriteLine = true;
            this.obDb = new AL_DBModule();
        }

        [TestMethod]
        public void TestMethod_Check_ReaderId_FromSAM_D()
        {
                       
            string uid = "04042B6A9F2980";//真實的uid
            string failUid = "123456";//錯誤的uid
            bool hasReaderId = false;
            bool hasNoneReaderId = true;
            this.obDb.OpenConnection();
            hasReaderId = this.shipment.Check_ReaderId_FromSAM_D(this.obDb, uid, "21");
            hasNoneReaderId = this.shipment.Check_ReaderId_FromSAM_D(this.obDb, failUid, "21");
            //有查到
            Assert.IsTrue(hasReaderId);
            //沒查到
            Assert.IsFalse(hasNoneReaderId);
        }
        [TestMethod]
        public void TestMethod_Shipment_Reader()
        {
            string uid = "04042B6A9F2980";
            string deviceId = "30CCFF0F04000000FE1ECD5DA64758E6";
            try
            {
                this.obDb.OpenConnection();
                this.shipment.Shipment_Reader(this.obDb, uid, deviceId);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }

        [TestMethod]
        public void TestMethod_Shipment_Reader_Define_MercFlg_and_ReaderType()
        {
            //使用自定義reader_type與顧客識別符號寫入db
            string uid = "04042B6A9F2980";
            string deviceId = "30CCFF0F04000000FE1ECD5DA64758E6";
            string reader_type = "02";
            string merc_flg = "CHT";
            try
            {
                this.obDb.OpenConnection();
                this.shipment.Shipment_Reader(this.obDb, uid, deviceId, reader_type, merc_flg);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        } 

        [TestCleanup]
        public void Clear()
        {
            if (this.obDb != null && this.obDb.ALCon != null)
            {
                this.obDb.CloseConnection();
            }
        }
    }
}
