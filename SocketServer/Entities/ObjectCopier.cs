using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
namespace SocketServer.Entities
{
    /// <summary>
    /// Clone POCO object
    /// </summary>
    public class ObjectCopier
    {
        /// <summary>
        /// 深層複製物件(包含參考一併複製)
        /// </summary>
        /// <typeparam name="T">物件型別</typeparam>
        /// <param name="source">要被複製的物件</param>
        /// <returns>Copy's object</returns>
        public static T Copy<T>(T source)
        {
            //檢查型別是否可序列化
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The Type must be Serializable.", "Source");
            }

            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            //建立二進位的formatter
            IFormatter formatter = new BinaryFormatter();
            //建立memoryStream來裝載轉換序列化後的資料
            Stream stream = new MemoryStream();
            using (stream)
            {
                //序列化物件到資料流內
                formatter.Serialize(stream, source);
                //設定資料流起始位置
                stream.Seek(0, SeekOrigin.Begin);
                //回傳還原序列化後的物件
                return (T)formatter.Deserialize(stream);
            }
            
        }

        public static T JSONCopy<T>(T source)
        {
            string jsonString = JsonConvert.SerializeObject(source);

            return (T)JsonConvert.DeserializeObject<T>(jsonString);
        }

    }
}
