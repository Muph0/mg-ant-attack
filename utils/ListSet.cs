using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zapoctak_antattack.utils
{
    /// <summary>
    /// Same as list, but contains every item only once.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class ListSet<T> : List<T>
    {
        public new void Add(T item)
        {
            if (!Contains(item))
            {
                base.Add(item);
            }
        }
    }
}
