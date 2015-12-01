using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Extensions
{
    public static class ListExtensions
    {
        public static bool IsInRange<T>(this List<T> obj, uint n)
        {
            if (obj == null)
            {
                return false;
            }
            else if (n < 0 || n >= obj.Count)
            {
                return false;
            }

            return true;
        }

        public static bool IsInRange<T>(this List<T> obj, uint start, uint length)
        {
            if (obj == null)
            {
                return false;
            }
            else if (!obj.IsInRange(start) || !obj.IsInRange(start + length - 1))
            {
                return false;
            }

            return true;
        }

    }
}
