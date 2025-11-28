using System.Buffers;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BBBirder.UnityVue")]
[assembly: InternalsVisibleTo("BBBirder.UnityVue.Editor")]


namespace BBBirder
{
    /// <summary>
    /// Shared properties belonging to current module.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static class ModuleProps<T>
    {
        public static readonly ArrayPool<T> ArrayPool = ArrayPool<T>.Create();
    }
}
