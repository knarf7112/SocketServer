using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//query string collection
using System.Web;
using System.Collections.Specialized;

namespace Test_Func
{
    public class AllPayGenMac
    {
        public delegate string AddSomeData(string originStr);
        //sort query string and dele add key and iv
        public static string SortQueryString(NameValueCollection querys, AddSomeData addKeyAndIV)
        {
            SortedDictionary<string, string> sort_dic = new SortedDictionary<string, string>();
            StringBuilder sb = new StringBuilder();
            string result = string.Empty;
            //sort collection
            foreach (string paraName in querys.Keys)
            {
                sort_dic.Add(paraName, querys[paraName]);
            }
            //combine to string
            foreach(string key in sort_dic.Keys){
                sb.AppendFormat("{0}={1}&",key, sort_dic[key]);
            }
            //remove last "&"
            sb = sb.Remove(sb.Length - 1, 1);
            if(addKeyAndIV != null)
            {
                result = addKeyAndIV.Invoke(sb.ToString());
            }
            else
            {
                result = sb.ToString();
            }
            return result;
        }

    }
}
