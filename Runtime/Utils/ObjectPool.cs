using System;
using System.Collections.Generic;

namespace BBBirder.UnityVue
{
    internal static class ObjectPool<T> where T : class, new()
    {
        internal static int MaxCount { get; set; } = 512;
        private readonly static Stack<T> _instances = new();
        public static T Get()
        {
            if (_instances.Count > 0)
            {
                return _instances.Pop();
            }
            return new();
        }

        public static void Recycle(T inst)
        {
            if (_instances.Count < MaxCount)
            {
                _instances.Push(inst);
            }
        }
    }
}
