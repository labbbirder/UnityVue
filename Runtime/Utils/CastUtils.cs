
using System;
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


        public static object DynamicCast(object v, Type targetType)
        {
            if (v == null) return null;
            var targetTypeCode = Type.GetTypeCode(targetType);
            return targetTypeCode switch
            {
                TypeCode.Boolean => Convert.ToBoolean(v),
                TypeCode.SByte => Convert.ToSByte(v),
                TypeCode.Int16 => Convert.ToInt16(v),
                TypeCode.Int32 => Convert.ToInt32(v),
                TypeCode.Int64 => Convert.ToInt64(v),
                TypeCode.Byte => Convert.ToByte(v),
                TypeCode.UInt16 => Convert.ToUInt16(v),
                TypeCode.UInt32 => Convert.ToUInt32(v),
                TypeCode.UInt64 => Convert.ToUInt64(v),
                TypeCode.Char => Convert.ToChar(v),
                TypeCode.String => Convert.ToString(v),
                TypeCode.Single => Convert.ToSingle(v),
                TypeCode.Double => Convert.ToDouble(v),
                TypeCode.Decimal => Convert.ToDecimal(v),
                TypeCode.DateTime => Convert.ToDateTime(v),
                _ => v,
            };
        }
    }
}
