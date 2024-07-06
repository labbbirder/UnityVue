using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BBBirder.UnityInjection;
using UnityEngine.Scripting;

namespace BBBirder.UnityVue
{
    /// <summary>
    /// Derive from me will make a normal object watchable
    /// </summary>
    public interface IDataProxy : IWatchable, IInjectionProvider
    {
        const BindingFlags bindingFlags = 0
            | BindingFlags.Instance
            | BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.DeclaredOnly
            ;
        static Dictionary<Type, Dictionary<string, Func<object, object>>> globalRawGetters = new();
        static Dictionary<Type, Dictionary<string, Action<object, object>>> globalRawSetters = new();
        static Dictionary<Type, HashSet<string>> globalWatchableProperties = new();

        bool IWatchable.IsPropertyWatchable(object key)
        {
            var sk = key is string s ? s : Convert.ToString(key);
            return IsPropertyWatchable(this.GetType(), sk);
        }

        static bool IsPropertyWatchable(Type type, string key)
        {
            if (type == null || !globalWatchableProperties.TryGetValue(type, out var watchableProperties))
            {
                return false;
            }
            if (watchableProperties.Contains(key))
            {
                return true;
            }
            else
            {
                return IsPropertyWatchable(type.BaseType, key);
            }
        }

        static bool TryGetInternal(IWatchable self, Type type, string key, out object value)
        {
            if (type == null || !globalRawGetters.TryGetValue(type, out var getters))
            {
                value = default;
                return false;
            }
            if (getters.TryGetValue(key, out var getter))
            {
                value = getter(self);
                return true;
            }
            else
            {
                return TryGetInternal(self, type.BaseType, key, out value);
            }
        }

        static bool TrySetInternal(IWatchable self, Type type, string key, object value)
        {
            if (type == null || !globalRawSetters.TryGetValue(type, out var setters))
            {
                return false;
            }
            if (setters.TryGetValue(key, out var setter))
            {
                setter(self, value);
                return true;
            }
            else
            {
                return TrySetInternal(self, type.BaseType, key, value);
            }
        }

        object IWatchable.RawGet(object key)
        {
            var sk = key is string s ? s : Convert.ToString(key);
            if (TryGetInternal(this, this.GetType(), sk, out var value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        bool IWatchable.RawSet(object key, object value)
        {
            var sk = key is string s ? s : Convert.ToString(key);
            return TrySetInternal(this, this.GetType(), sk, value);
        }

        [Preserve]
        Func<C, T> ProxyGet<C, T>(string name, Func<C, T> rawGetter, object _) where C : IWatchable
        {
            var thisType = this.GetType();
            if (!globalRawGetters.TryGetValue(thisType, out var getters))
            {
                globalRawGetters[thisType] = getters = new();
            }
            getters[name] = (object o) => rawGetter((C)o);

            return o =>
            {
                o.onPropertyGet?.Invoke(name);
                return rawGetter.Invoke(o);
            };
        }

        [Preserve]
        Action<C, T> ProxySet<C, T>(string name, Action<C, T> rawSetter, Func<C, T> rawGetter) where C : IWatchable
        {
            var thisType = this.GetType();
            if (!globalRawSetters.TryGetValue(thisType, out var setters))
            {
                globalRawSetters[thisType] = setters = new();
            }
            setters[name] = (object o, object v) => rawSetter((C)o, (T)v);

            var comparer = EqualityComparer<T>.Default;
            return (o, v) =>
            {
                if (comparer.Equals(v, rawGetter(o))) return;
                rawSetter.Invoke(o, v);
                o.onPropertySet?.Invoke(name);
            };
        }

        static IEnumerable<PropertyInfo> GetValidProperties(Type targetType)
        {
            if (targetType.IsAbstract || targetType.IsInterface)
                yield break;

            foreach (var property in targetType.GetProperties(bindingFlags))
            {
                if (property.GetCustomAttribute<IgnoreProxyPropertyAttribute>() != null)
                {
                    continue;
                }
                if (property.PropertyType.GetCustomAttribute<IgnoreProxyPropertyAttribute>() != null)
                {
                    continue;
                }
                if (property.PropertyType.IsSubclassOf(typeof(Delegate)))
                {
                    continue;
                }
                if (!property.CanRead || !property.CanWrite)
                {
                    continue;
                }

                yield return property;
            }
        }

        IEnumerable<InjectionInfo> IInjectionProvider.ProvideInjections()
        {
            var proxyGet = typeof(IDataProxy).GetMethod(nameof(ProxyGet), bindingFlags);
            var proxySet = typeof(IDataProxy).GetMethod(nameof(ProxySet), bindingFlags);

            var targetType = this.GetType();
            var args = new object[3];

            foreach (var property in GetValidProperties(targetType))
            {
                var name = property.Name;

                if (typeof(IWatchable).IsAssignableFrom(property.PropertyType))
                {
                    if (!globalWatchableProperties.TryGetValue(targetType, out var watchableProperties))
                    {
                        globalWatchableProperties[targetType] = watchableProperties = new();
                    }
                    watchableProperties.Add(name);
                }

                var instGetMethod = proxyGet.MakeGenericMethod(targetType, property.PropertyType);
                if (property.GetMethod != null)
                {
                    yield return new InjectionInfo(property.GetMethod, raw =>
                    {
                        args[0] = name;
                        args[1] = Delegate.CreateDelegate(instGetMethod.GetParameters()[1].ParameterType, raw.Method);
                        args[2] = null;
                        return instGetMethod.Invoke(this, args) as MulticastDelegate;
                    });
                }

                var instSetMethod = proxySet.MakeGenericMethod(targetType, property.PropertyType);
                if (property.SetMethod != null)
                {
                    yield return new InjectionInfo(property.SetMethod, raw =>
                    {
                        args[0] = name;
                        args[2] = args[1];
                        args[1] = Delegate.CreateDelegate(instSetMethod.GetParameters()[1].ParameterType, raw.Method);
                        // args[1] = raw;
                        return instSetMethod.Invoke(this, args) as MulticastDelegate;
                    });
                }
            }
        }
    }

}
