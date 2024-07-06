
using System.Buffers;
using System.Runtime.CompilerServices;

namespace BBBirder.UnityVue
{
    internal static class CollectionUtility<T>
    {
        public static readonly ArrayPool<T> ArrayPool = ArrayPool<T>.Shared;
        public unsafe static int CeilExponent(int n)
        {
            float f = n;
            var exp = (int)(*(uint*)&f << 1 >> 24) - 127;
            // var exp = (*(int*)&f << 1 >>> 24) - 127; // C# 11.0
            if (1 << exp < n) exp++;
            return exp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LargerSizeInPowOf2(int cnt) => 1 << CeilExponent(cnt);
    }
}
