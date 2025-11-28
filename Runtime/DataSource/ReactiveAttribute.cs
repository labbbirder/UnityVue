using System;
using System.Collections.Generic;
using System.Reflection;
using BBBirder.UnityInjection;
using UnityEngine;

namespace BBBirder.UnityVue
{
    /// <summary>
    /// Counterpart of ReactiveAttribute. Notify the corresponding property when field value changed in InspectorWindow.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class NotifyAttribute : Attribute
    {
        internal string TargetProperty { get; set; }
        public NotifyAttribute(string targetProperty)
        {
            TargetProperty = targetProperty;
        }
    }

    /// <summary>
    /// Mark on arbitrary property to make it watchable
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ReactiveAttribute : InjectionAttribute
    {
        static MethodInfo s_proxyGet, s_proxySet;
        internal static Dictionary<Type, Dictionary<object, Func<IWatchable, object>>> globalRawGetters = new();
        internal static Dictionary<Type, Dictionary<object, Action<IWatchable, object>>> globalRawSetters = new();

        internal static bool TryGetInternal(IWatchable self, object key, out object value)
        {
            return TryGetInternal(self, self.GetType(), key, out value);
        }

        internal static bool TrySetInternal(IWatchable self, object key, object value)
        {
            return TrySetInternal(self, self.GetType(), key, value);
        }

        static bool TryGetInternal(IWatchable self, Type type, object key, out object value)
        {
            if (type == null)
            {
                value = default;
                return false;
            }

            if (globalRawGetters.TryGetValue(type, out var getters) && getters.TryGetValue(key, out var getter))
            {
                value = getter(self);
                return true;
            }
            else
            {
                return TryGetInternal(self, type.BaseType, key, out value);
            }
        }

        static bool TrySetInternal(IWatchable self, Type type, object key, object value)
        {
            if (type == null)
            {
                return false;
            }

            if (globalRawSetters.TryGetValue(type, out var setters) && setters.TryGetValue(key, out var setter))
            {
                setter(self, value);
                return true;
            }
            else
            {
                return TrySetInternal(self, type.BaseType, key, value);
            }
        }

        static Func<TKlass, TValue> ProxyGet<TKlass, TValue>(object key, Func<TKlass, TValue> rawGetter) where TKlass : IWatchable
        {
            if (!globalRawGetters.TryGetValue(typeof(TKlass), out var rawGetters))
            {
                globalRawGetters[typeof(TKlass)] = rawGetters = new();
            }

            rawGetters[key] = watchable => rawGetter((TKlass)watchable);
            return watchable =>
            {
                watchable.Payload.onBeforeGet?.Invoke(watchable, key);
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
            return OnSetProperty;

            void OnSetProperty(TKlass watchable, TValue value)
            {
                if (comparer.Equals(value, rawGetter(watchable))) return;
                rawSetter(watchable, value);
                watchable.Payload.onAfterSet?.Invoke(watchable, key);
            }
        }

        public override IEnumerable<InjectionInfo> ProvideInjections()
        {
            var property = TargetMember as PropertyInfo;
            var klassType = property.DeclaringType;
            if (!typeof(IWatchable).IsAssignableFrom(klassType))
            {
                Debug.LogError($"property {TargetMember} marked by ReactiveAttribute, but its declaring type {klassType} is not IWatchable.");
                yield break;
            }

            if (property.GetMethod?.IsStatic == true || property.SetMethod?.IsStatic == true)
            {
                Debug.LogError($"property {TargetMember} marked by ReactiveAttribute, but it is static.");
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
