using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using com.bbbirder;

namespace BBBirder.UnityVue
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public abstract class BindableAttribute : DirectRetrieveAttribute
    {
        public ScopeFlushMode FlushMode;
        // protected WatchScope scope;

        [DebuggerHidden]
        abstract protected WatchScope EnableInternal(IDataBinder binder);

        [DebuggerHidden]
        internal void Enable(IDataBinder binder)
        {
            var scope = EnableInternal(binder);
            if (scope is null) return;
            scope.debugName = targetInfo.DeclaringType.Name + "." + targetInfo.Name;
            scope.SetFlushMode(FlushMode);
            binder.m_AttributeScopes.Add(scope);
        }

        // internal void Disable()
        // {
        //     scope?.Dispose();
        //     scope = null;
        // }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class WatchAttribute : BindableAttribute
    {
        /// <summary>
        /// force `.Update()` on enable
        /// </summary>
        public bool UpdateOnce;

        [DebuggerHidden]
        override protected WatchScope EnableInternal(IDataBinder binder)
        {
            var property = targetInfo as PropertyInfo;
            var scp = CSReactive.Watch(() => property.GetValue(binder), () =>
                {
                    if (binder.IsBinded)
                    {
                        property.SetValue(binder, property.GetValue(binder));
                    }
                })
                .WithLifeKeeper(binder);
            if (UpdateOnce) scp.Update();
            return scp;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ComputedAttribute : BindableAttribute
    {
        [DebuggerHidden]
        override protected WatchScope EnableInternal(IDataBinder binder)
        {
            var property = targetInfo as PropertyInfo;
            return CSReactive.Watch(() => property.GetValue(binder),
                () => property.SetValue(binder, property.GetValue(binder)))
                .WithLifeKeeper(binder);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class WatchEffectAttribute : BindableAttribute
    {
        [DebuggerHidden]
        override protected WatchScope EnableInternal(IDataBinder binder)
        {
            var method = targetInfo as MethodInfo;
            return CSReactive.WatchEffect(() =>
                {
                    if (binder.IsBinded)
                    {
                        method.Invoke(binder, null);
                    }
                })
                .WithLifeKeeper(binder);
        }
    }

    public static class BindAttributeExtension
    {
        static Dictionary<Type, BindableAttribute[]> lutAttributes = new();

        internal static BindableAttribute[] GetBindAttributes(this IDataBinder binder)
        {

            var binderType = binder.GetType();
            if (!lutAttributes.TryGetValue(binderType, out var attrs))
            {
                lutAttributes[binderType] = attrs = Retriever.GetAllAttributes<BindableAttribute>(binderType.Assembly)
                    .Where(a => (a.targetInfo is Type t && IsTypeOfSubtypeOf(binderType, t))
                        || (a.targetInfo is not Type && IsTypeOfSubtypeOf(binderType, a.targetInfo.DeclaringType)))
                    .ToArray()
                    ;
            }
            return attrs;
            static bool IsTypeOfSubtypeOf(Type subType, Type baseType)
            {
                if (subType == baseType) return true;
                if (subType.IsSubclassOf(baseType)) return true;
                return false;
            }
        }

    }
}
