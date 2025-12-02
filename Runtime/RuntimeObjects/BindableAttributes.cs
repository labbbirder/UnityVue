using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BBBirder.DirectAttribute;
using BBBirder.UnityInjection;

namespace BBBirder.UnityVue
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public abstract class BindableAttribute : DirectRetrieveAttribute
    {
        public ScopeFlushMode FlushMode;
#if ENABLE_UNITY_VUE_TRACKER
        protected readonly string fp;
        protected readonly int ln;
        public BindableAttribute([CallerFilePath] string fp = null, [CallerLineNumber] int ln = 0)
        {
            this.fp = fp;
            this.ln = ln;
        }
#endif
        [DebuggerHidden]
        abstract protected WatchScope EnableInternal(IDataBinder binder);

        [DebuggerHidden]
        internal void Enable(IDataBinder binder)
        {
            try
            {
                var scope = EnableInternal(binder);
                if (scope is null) return;
#if ENABLE_UNITY_VUE_TRACKER
                scope.debugName = $"{targetInfo.DeclaringType.Name}:{targetInfo.Name} (at {fp}:{ln})";
                // scope.stackFrames = scope.stackFrames.Prepend(new StackFrame(fp, ln)).ToArray();
#endif
                // binder.ScopesFromMember.Add(scope);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        // internal void Disable()
        // {
        //     scope?.Dispose();
        //     scope = null;
        // }
    }

    public struct WatchArgument
    {
        public object curr, prev;
        public WatchArgument(object curr, object prev)
        {
            this.curr = curr;
            this.prev = prev;
        }

        public void Deconstruct(out object curr, out object prev)
        {
            curr = this.curr;
            prev = this.prev;
        }

    }

    public struct WatchArgument<T> : IEquatable<WatchArgument<T>>
    {
        public T curr, prev;
        public WatchArgument(T curr, T prev)
        {
            this.curr = curr;
            this.prev = prev;
        }

        public void Deconstruct(out T curr, out T prev)
        {
            curr = this.curr;
            prev = this.prev;
        }

        public bool Equals(WatchArgument<T> other)
        {
            return EqualityComparer<T>.Default.Equals(curr, other.curr);
        }

        public static implicit operator WatchArgument<T>(T value)
        {
            return new()
            {
                curr = value,
            };
        }

        public static implicit operator WatchArgument<T>(WatchArgument value)
        {
            return new((T)value.curr, (T)value.prev);
        }

        public static implicit operator WatchArgument(WatchArgument<T> value)
        {
            return new((T)value.curr, (T)value.prev);
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class WatchAttribute : BindableAttribute
    {
        static readonly Dictionary<MemberInfo, Func<IDataBinder, WatchScope>> s_scopeFactories = new();
        static MethodInfo s_miCreateWatchScope;

        static Func<IDataBinder, WatchScope> CreateWatchScope<T>(PropertyInfo property, ScopeFlushMode flushMode, bool flushOnStartup, bool useArgument)
        {
            if (useArgument)
            {
                var getter = property.DeclaringType.GetMemberGetter<WatchArgument<T>>(property.Name);
                var setter = property.DeclaringType.GetMemberSetter<WatchArgument<T>>(property.Name);

                return (IDataBinder binder) =>
                {
                    var scp = binder.Watch(RunCheck, RunEffect, flushMode);
                    if (flushOnStartup) scp.Update();
                    scp.SetDebugName(property.Name);
                    return scp;

                    WatchArgument<T> RunCheck()
                    {
                        return getter.Invoke(binder);
                    }

                    void RunEffect(WatchArgument<T> curr, WatchArgument<T> prev)
                    {
                        setter.Invoke(binder, new WatchArgument<T>(curr.curr, prev.curr));
                    }
                };

            }
            else
            {
                var getter = property.DeclaringType.GetMemberGetter<T>(property.Name);
                var setter = property.DeclaringType.GetMemberSetter<T>(property.Name);

                return (IDataBinder binder) =>
                {
                    var scp = binder.Watch(RunCheck, RunEffect, flushMode);
                    if (flushOnStartup) scp.Update();
                    scp.SetDebugName(property.Name);
                    return scp;

                    T RunCheck()
                    {
                        return getter.Invoke(binder);
                    }

                    void RunEffect(T value)
                    {
                        setter.Invoke(binder, value);
                    }
                };

            }
        }

        static WatchAttribute()
        {
            s_miCreateWatchScope = typeof(WatchAttribute).GetMethod(nameof(CreateWatchScope), BindingFlags.Static | BindingFlags.NonPublic);
        }
        /// <summary>
        /// force `.Update()` on enable
        /// </summary>
        public bool FlushOnStartup;

#if ENABLE_UNITY_VUE_TRACKER
        public WatchAttribute([CallerFilePath] string fp = null, [CallerLineNumber] int ln = 0)
            : base(fp, ln)
        { }
#endif

        public WatchAttribute(ScopeFlushMode flushMode = ScopeFlushMode.PostUpdate, bool flushOnStartup = false)
        {
            this.FlushMode = flushMode;
            this.FlushOnStartup = flushOnStartup;
        }

        private Func<IDataBinder, WatchScope> GetOrCreateFactory(MemberInfo member)
        {
            if (!s_scopeFactories.TryGetValue(member, out var factory))
            {
                var property = member as PropertyInfo;
                var useArgument = property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(WatchArgument<>);
                var valueType = useArgument ? property.PropertyType.GenericTypeArguments[0] : property.PropertyType;
                s_scopeFactories[member] = factory = s_miCreateWatchScope
                    .MakeGenericMethod(valueType)
                    .Invoke(null, new object[] { property, FlushMode, FlushOnStartup, useArgument }) as Func<IDataBinder, WatchScope>;
            }

            return factory;
        }

        [DebuggerHidden]
        override protected WatchScope EnableInternal(IDataBinder binder)
        {
            var factory = GetOrCreateFactory(TargetMember);
            return factory.Invoke(binder);
        }
    }

    // [AttributeUsage(AttributeTargets.Property)]
    // public class ComputedAttribute : BindableAttribute
    // {
    //     [DebuggerHidden]
    //     override protected WatchScope EnableInternal(IDataBinder binder)
    //     {
    //         var property = targetInfo as PropertyInfo;
    //         return CSReactive.Watch(() => property.GetValue(binder), () =>
    //             {
    //                 if (binder.IsBinded)
    //                 {
    //                     property.SetValue(binder, property.GetValue(binder));
    //                 }
    //             })
    //             .WithLifeKeeper(binder);
    //     }
    // }

    [AttributeUsage(AttributeTargets.Method)]
    public class WatchEffectAttribute : BindableAttribute
    {
#if ENABLE_UNITY_VUE_TRACKER
        public WatchEffectAttribute([CallerFilePath] string fp = null, [CallerLineNumber] int ln = 0)
            : base(fp, ln)
        { }
#endif

        public WatchEffectAttribute(ScopeFlushMode flushMode = ScopeFlushMode.PostUpdate)
        {
            this.FlushMode = flushMode;
        }

        [DebuggerHidden]
        override protected WatchScope EnableInternal(IDataBinder binder)
        {
            var method = TargetMember as MethodInfo;
            var action = method.CreateDelegate(typeof(Action), binder) as Action;
            var scp = binder.WatchEffect(() =>
                {
                    action();
                }, FlushMode);
            scp.SetDebugName(method.Name);
            return scp;
        }
    }

    public static class BindAttributeExtension
    {
        static BindableAttribute[] allAttributes;
        static Dictionary<Type, BindableAttribute[]> lutAttributes = new();

        internal static BindableAttribute[] GetBindAttributes(this IDataBinder binder)
        {
            var binderType = binder.GetType();
            if (!lutAttributes.TryGetValue(binderType, out var attrs))
            {
                allAttributes ??= Retriever.GetAllAttributes<BindableAttribute>().ToArray();
                lutAttributes[binderType] = attrs = allAttributes
                    .Where(a => (a.TargetMember is Type t && IsTypeOfSubtypeOf(binderType, t)) // marked on base type
                        || (a.TargetMember is not Type && IsTypeOfSubtypeOf(binderType, a.TargetMember.DeclaringType)) // marked on member of base type
                    )
                    .ToArray()
                    ;
            }

            return attrs;
        }

        static bool IsTypeOfSubtypeOf(Type subType, Type baseType)
        {
            if (subType == baseType) return true;
            if (subType.IsSubclassOf(baseType)) return true;
            return false;
        }
    }
}
