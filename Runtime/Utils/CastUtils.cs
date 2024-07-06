
using System.Linq;
using System.Runtime.CompilerServices;

namespace BBBirder.UnityVue
{
    public static class CastUtils
    {
        const int MIN_BOX_NUMBER = -256;
        const int MAX_BOX_NUMBER = 512;
        public static object[] boxed_numbers;

        static CastUtils()
        {
            boxed_numbers = new object[MAX_BOX_NUMBER - MIN_BOX_NUMBER];
            for (int i = MIN_BOX_NUMBER; i < MAX_BOX_NUMBER; i++)
            {
                boxed_numbers[i - MIN_BOX_NUMBER] = i;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object BoxNumber(this int n)
        {
            if (n >= MAX_BOX_NUMBER || n < MIN_BOX_NUMBER)
            {
                return n;
            }
            else
            {
                return boxed_numbers[n - MIN_BOX_NUMBER];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object BoxNumber(this uint n) => BoxNumber((int)n);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object BoxNumber(this long n) => BoxNumber((int)n);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object BoxNumber(this ulong n) => BoxNumber((int)n);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object BoxNumber(this byte n) => BoxNumber((int)n);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object BoxNumber(this sbyte n) => BoxNumber((int)n);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object BoxNumber(this short n) => BoxNumber((int)n);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object BoxNumber(this ushort n) => BoxNumber((int)n);
    }
}
