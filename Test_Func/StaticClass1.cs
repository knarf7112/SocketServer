using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Collections.Concurrent;

namespace Test_Func.Test
{
    public static class StaticClass1
    {
        private static ConcurrentBag<int> threadSafeList = new ConcurrentBag<int>();
        private static IList<int> list = new List<int>();
        private static object lockObj = new object();
        public static void Add(int i)
        {
            //lock (lockObj)
            //{
                list.Add(i);
            //}
        }

        public static void DisplayCount()
        {
            Console.WriteLine(list.Count());
        }
    }

    public class TestClass1
    {
        public static IList<string> StrList = new List<string>();

        public static string Str = "test123";

        public static int Intarger = 999;

        public static IList<int> IntList = new List<int>();

        public static void AddStr()
        {
            var tmp = "a";
            for (var i = 1; i <= 10; i++)
            {

                StrList.Add(tmp);
                byte[] bytes = Encoding.ASCII.GetBytes(tmp);
                bytes[0] += 1;// (byte)i;
                tmp = Encoding.ASCII.GetString(bytes);
            }

        }
    }
}
