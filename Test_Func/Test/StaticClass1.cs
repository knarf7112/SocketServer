using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_Func
{
    public static class StaticClass1
    {
        private static IList<int> list = new List<int>();
        private static object lockObj = new object();
        public static void Add(int i)
        {
            lock (lockObj)
            {
                list.Add(i);
            }
        }

        public static void DisplayCount()
        {
            Console.WriteLine(list.Count());
        }
    }
}
