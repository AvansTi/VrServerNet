using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VrServer
{
    public static class Util
    {
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
}
