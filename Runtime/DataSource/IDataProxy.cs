using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BBBirder.UnityInjection;

namespace BBBirder.UnityVue
{
    /// <summary>
    /// Derive from me will make a normal object watchable (only includes properties in non-abstract subtypes)
    /// </summary>
    public interface IDataProxy : IWatchable, IInjectionProvider
    {
        const BindingFlags PropertyBindingFlags = 0
            | BindingFlags.Instance
            | BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.DeclaredOnly
            ;
        const BindingFlags ProxyBindingFlags = 0
            | BindingFlags.Static
            | BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.DeclaredOnly
            ;

        static Dictionary<Type, Dictionary<string, Func<object, object>>> globalRawGetters = new();
        static Dictionary<Type, Dictionary<string, Action<object, object>>> globalRawSetters = new();
        static Dictionary<Type, HashSet<string>> globalWatchableProperties = new();
        static object[] s_args = new object[4];
        static MethodInfo s_proxyGet, s_proxySet;
        static Regex s_Varname = new(@"^[_a-zA-Z][_a-zA-Z0-9]*$");

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

        static bool TrySetInternal(IWatchable self, Type type, string key, object value)
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

        static Func<TKlass, TProp> ProxyGet<TKlass, TProp>(Type thisType, string name, Func<TKlass, TProp> rawGetter, object _) where TKlass : IWatchable
        {
            if (!globalRawGetters.TryGetValue(thisType, out var getters))
            {
                globalRawGetters[thisType] = getters = new();
            }

            getters[name] = (object o) => rawGetter((TKlass)o);

            return o =>
            {
                o.Payload.onBeforeGet?.Invoke(o, name);
                return rawGetter.Invoke(o);
            };
        }

        static Action<TKlass, TProp> ProxySet<TKlass, TProp>(Type thisType, string name, Action<TKlass, TProp> rawSetter, Func<TKlass, TProp> rawGetter) where TKlass : IWatchable
        {
            if (!globalRawSetters.TryGetValue(thisType, out var setters))
            {
                globalRawSetters[thisType] = setters = new();
            }

            setters[name] = (object o, object v) => rawSetter((TKlass)o, (TProp)v);

            var comparer = EqualityComparer<TProp>.Default;
            return (o, v) =>
            {
                if (comparer.Equals(v, rawGetter(o))) return;
                rawSetter.Invoke(o, v);
                o.Payload.onAfterSet?.Invoke(o, name);
            };
        }

        public static bool IsPropertyValid(PropertyInfo property)
        {
            if (property.GetCustomAttribute<IgnoreProxyAttribute>() != null)
            {
                return false;
            }

            if (property.PropertyType.GetCustomAttribute<IgnoreProxyAttribute>() != null)
            {
                return false;
            }

            if (property.PropertyType.IsSubclassOf(typeof(Delegate)))
            {
                return false;
            }

            if (!property.CanRead || !property.CanWrite)
            {
                return false;
            }

            if (property.Name is "Item")
            {
                return false;
            }

            if (!s_Varname.IsMatch(property.Name))
            {
                return false;
            }

            return true;
        }

        public static IEnumerable<PropertyInfo> GetValidProperties(Type targetType)
        {
            for (var baseType = targetType; baseType != null && typeof(IDataProxy).IsAssignableFrom(baseType); baseType = baseType.BaseType)
            {
                // Do traverse abstract base types, base type has no opportunity to execute.
                if (!baseType.IsAbstract && baseType != targetType) continue;
                foreach (var property in baseType.GetProperties(PropertyBindingFlags))
                {
                    if (IsPropertyValid(property))
                        yield return property;
                }
            }
        }

        public static IEnumerable<InjectionInfo> ProvideInjectionsForProperty(Type thisType, PropertyInfo property)
        {
            s_proxyGet ??= typeof(IDataProxy).GetMethod(nameof(ProxyGet), ProxyBindingFlags);
            s_proxySet ??= typeof(IDataProxy).GetMethod(nameof(ProxySet), ProxyBindingFlags);
            var targetType = property.DeclaringType;
            var name = property.Name;

            if (typeof(IWatchable).IsAssignableFrom(property.PropertyType))
            {
                if (!globalWatchableProperties.TryGetValue(targetType, out var watchableProperties))
                {
                    globalWatchableProperties[targetType] = watchableProperties = new();
                }

                watchableProperties.Add(name);
            }

            var instGetMethod = s_proxyGet.GetGenericMethodDefinition().MakeGenericMethod(targetType, property.PropertyType);
            if (property.GetMethod != null)
            {
                yield return new InjectionInfo(property.GetMethod, raw =>
                {
                    s_args[0] = thisType;
                    s_args[1] = name;
                    s_args[2] = Delegate.CreateDelegate(instGetMethod.GetParameters()[2].ParameterType, raw.Method);
                    s_args[3] = null;
                    return instGetMethod.Invoke(null, s_args) as MulticastDelegate;
                });
            }

            var instSetMethod = s_proxySet.GetGenericMethodDefinition().MakeGenericMethod(targetType, property.PropertyType);
            if (property.SetMethod != null)
            {
                yield return new InjectionInfo(property.SetMethod, raw =>
                {
                    s_args[0] = thisType;
                    s_args[1] = name;
                    s_args[3] = s_args[2];
                    s_args[2] = Delegate.CreateDelegate(instSetMethod.GetParameters()[2].ParameterType, raw.Method);
                    // args[1] = raw;
                    return instSetMethod.Invoke(null, s_args) as MulticastDelegate;
                });
            }
        }

        IEnumerable<InjectionInfo> IInjectionProvider.ProvideInjections()
        {
            var targetType = this.GetType();

            foreach (var property in GetValidProperties(targetType))
            {
                foreach (var injectionInfo in ProvideInjectionsForProperty(targetType, property))
                {
                    yield return injectionInfo;
                }
            }
        }

        /// <summary>
        /// Trigger property change callbacks without equality check.
        /// </summary>
        /// <param name="propertyKey"></param>
        public void ForceNotifyPropertyChange(object propertyKey)
        {
            Payload.onAfterSet?.Invoke(this, propertyKey);
        }
    }

    public static class DataProxyExtensions
    {
        /// <summary>
        /// Trigger property change callbacks without equality check.
        /// </summary>
        /// <param name="propertyKey"></param>
        public static void ForceNotifyPropertyChange(this IDataProxy proxy, object propertyKey)
        {
            proxy.Payload.onAfterSet?.Invoke(proxy, propertyKey);
        }
    }



}
