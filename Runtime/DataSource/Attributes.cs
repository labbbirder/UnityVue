using System;
using System.Collections.Generic;
using System.Reflection;
using BBBirder.UnityInjection;
using UnityEngine;
namespace BBBirder.UnityVue
{
    [Flags]
    public enum AccessibilityLevel
    {
        None = 0,
        Private = 1,
        Internal = 2,
        Public = 4,
        All = -1
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ExportFieldsAttribute : Attribute
    {
        public AccessibilityLevel AccessibilityLevel { get; set; } = AccessibilityLevel.All;
        public string MatchName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ExportIgnoreAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class ReactiveAttribute : InjectionAttribute
    {
        static MethodInfo s_proxyGet, s_proxySet;
        internal static Dictionary<Type, Dictionary<object, Func<IWatchable, object>>> globalRawGetters = new();
        internal static Dictionary<Type, Dictionary<object, Action<IWatchable, object>>> globalRawSetters = new();

        static Func<TKlass, TValue> ProxyGet<TKlass, TValue>(object key, Func<TKlass, TValue> rawGetter) where TKlass : IWatchable
        {
            if (!globalRawGetters.TryGetValue(typeof(TKlass), out var rawGetters))
            {
                globalRawGetters[typeof(TKlass)] = rawGetters = new();
            }
            rawGetters[key] = watchable => rawGetter((TKlass)watchable);
            return watchable =>
            {
                watchable.onPropertyGet?.Invoke(watchable, key);
                return rawGetter(watchable);
            };
        }

        static Action<TKlass, TValue> ProxySet<TKlass, TValue>(object key, Action<TKlass, TValue> rawSetter, Func<TKlass, TValue> rawGetter) where TKlass : IWatchable
        {
            if (!globalRawSetters.TryGetValue(typeof(TKlass), out var rawSetters))
            {
                globalRawSetters[typeof(TKlass)] = rawSetters = new();
            }
            rawSetters[key] = (watchable, value) => rawSetter((TKlass)watchable, (TValue)value);

            var comparer = EqualityComparer<TValue>.Default;
            return (watchable, value) =>
            {
                if (comparer.Equals(value, rawGetter(watchable))) return;
                rawSetter(watchable, value);
                watchable.onPropertySet?.Invoke(watchable, key);
            };
        }

        public override IEnumerable<InjectionInfo> ProvideInjections()
        {
            var property = targetInfo as PropertyInfo;
            var klassType = property.DeclaringType;
            if (!typeof(IWatchable).IsAssignableFrom(klassType))
            {
                Debug.LogError($"property {targetInfo} marked by ReactiveAttribute, but its declaring type is not IWatchable.");
                yield break;
            }

            s_proxyGet ??= typeof(ReactiveAttribute).GetMethod(nameof(ProxyGet), BindingFlags.Static | BindingFlags.NonPublic);
            s_proxySet ??= typeof(ReactiveAttribute).GetMethod(nameof(ProxySet), BindingFlags.Static | BindingFlags.NonPublic);

            var proxyGet = s_proxyGet.MakeGenericMethod(klassType, property.PropertyType);
            var proxySet = s_proxySet.MakeGenericMethod(klassType, property.PropertyType);

            Delegate temp_rawGetter = default;
            yield return new InjectionInfo(property.GetMethod, rawGetter =>
            {
                temp_rawGetter = rawGetter;
                return proxyGet.Invoke(null, new object[] {
                    property.Name,
                    Delegate.CreateDelegate(proxySet.GetParameters()[2].ParameterType, rawGetter.Method)
                }) as MulticastDelegate;
            });

            yield return new InjectionInfo(property.SetMethod, rawSetter =>
            {
                return proxySet.Invoke(null, new object[] {
                    property.Name,
                    Delegate.CreateDelegate(proxySet.GetParameters()[1].ParameterType, rawSetter.Method),
                    Delegate.CreateDelegate(proxySet.GetParameters()[2].ParameterType, temp_rawGetter.Method)
                }) as MulticastDelegate;
            });
        }
    }
}
