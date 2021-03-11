using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace zapoctak_antattack.utils
{
    static class VectorExtensions
    {
        public static Vector3 Rounded(this Vector3 vec)
        {
            return new Vector3((float)Math.Round(vec.X), (float)Math.Round(vec.Y), (float)Math.Round(vec.Z));
        }
        public static (int,int,int) ToTuple(this Vector3 vec)
        {
            var r = vec.Rounded();
            return ((int)r.X, (int)r.Y, (int)r.Z);
        }
        public static Vector2 ToVector2(this Vector3 vec)
        {
            return new Vector2(vec.X, vec.Y);
        }

        public static Vector2 Rounded(this Vector2 vec)
        {
            return new Vector2((float)Math.Round(vec.X), (float)Math.Round(vec.Y));
        }

        public static Rectangle Scaled(this Rectangle rect, int scale)
        {
            return new Rectangle(rect.X * scale, rect.Y * scale, rect.Width * scale, rect.Height * scale);
        }
    }
}
