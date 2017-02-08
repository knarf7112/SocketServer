using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_Func.Test
{
    //ref: http://blog.darkthread.net/post-2016-08-24-linq-groupby-todictionary-grouping.aspx
    public enum Teams
    {
        Valor,
        Mystic,
        Instinct,
        Dark
    }

    public class Trainer
    {
        public Teams Team;

        public string Name;

        public Trainer(Teams team, string name)
        {
            Team = team; Name = name;
        }
    }
    public class Test_LinqGroup
    {
        public static void RunTest()
        {
            //來源資料如下
            List<Trainer> trainers = new List<Trainer>(){
                new Trainer(Teams.Valor, "Candela"),
                new Trainer(Teams.Valor, "Bob"),
                new Trainer(Teams.Mystic, "Blanche"),
                new Trainer(Teams.Valor, "Alice"),
                new Trainer(Teams.Instinct, "Spark"),
                new Trainer(Teams.Mystic, "Tom"),
                new Trainer(Teams.Dark, "Jeffrey")
            };

            //目標：以Team分類，將同隊的訓練師集合成List<Trainer>，
            //最終產出Dictionary<Teams, List<Trainer>>

            //以前的寫法，跑迴圈加邏輯比對
            var res1 = new Dictionary<Teams, List<Trainer>>();
            foreach (var t in trainers)
            {
                //若無key,建立key的空列表清單
                if (!res1.ContainsKey(t.Team))
                {
                    res1.Add(t.Team, new List<Trainer>());
                }
                //將同Team分類的新增進入List
                res1[t.Team].Add(t);
            }

            //新寫法，使用LINQ GroupBy
            var res2 = trainers.GroupBy(o => o.Team)
                .ToDictionary(obj => obj.Key, obj => obj.ToList());
                //.ToDictionary(obj => obj.Key);//沒有ToList則後面Value不會實體化
            
           
            foreach(var item in res2)
            {
                item.Value.ForEach(v=>
                {
                    Console.WriteLine(item.Key.ToString() + ":" + v.Team.ToString() + ":" + v.Name);
                }); 
            }

            Console.ReadKey();
        }
    }
}
