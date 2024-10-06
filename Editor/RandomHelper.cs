using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor
{
    internal static class RandomHelper
    {
        private static Random random = new Random();

        public static float GetRandomFloat(float min, float max)
        {
            return (float)(random.NextDouble() * (max - min) + min);
        }
    }
}