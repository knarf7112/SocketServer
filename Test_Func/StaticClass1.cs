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
}
