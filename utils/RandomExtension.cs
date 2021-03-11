using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zapoctak_antattack.utils
{
    static class RandomExtension
    {
        public static T Select<T>(this Random rnd, IList<T> list)
        {
            if (list == null || list.Count == 0) throw new ArgumentException();
            int index = rnd.Next(list.Count);
            return list[index];
        }     
    }
}
