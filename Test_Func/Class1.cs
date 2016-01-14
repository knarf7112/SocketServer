using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_Func
{
    internal class Class1
    {
        public void add(int i)
        {
            StaticClass1.Add(i);
        }
    }


    internal class ClsTest2
    {
        public static IList<int> list;


        public ClsTest2()
        {
            ClsTest2.list = new List<int>();
        }


        public void Add(int i)
        {
            ClsTest2.list.Add(i);
        }
    }
}
