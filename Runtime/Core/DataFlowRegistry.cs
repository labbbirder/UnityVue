using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using BBBirder.DirectAttribute;
using BBBirder.UnityVue;

namespace BBBirder
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DataSourceAttribute : DirectRetrieveAttribute
    {
        public readonly string DataName;
        public DataSourceAttribute(string dataName)
        {
            this.DataName = dataName;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DataTargetAttribute : DirectRetrieveAttribute
    {
        public readonly string DataName;
        public DataTargetAttribute(string dataName)
        {
            this.DataName = dataName;
        }
    }

    internal static class DataFlowRegistry
    {
        /*
        Member --> Type (plus baseTypes...)

        */
        const BindingFlags HelperFlags = BindingFlags.Static | BindingFlags.NonPublic;
        static Dictionary<MemberInfo, Dictionary<int, Action<object, object>>> s_flows = new();
        static Dictionary<int, (List<DataSourceAttribute> sources, Dictionary<string, List<DataTargetAttribute>> targets)> s_pipeEnds = new();
        static MethodInfo s_miBindHelper;

        static DataFlowRegistry()
        {
            // collect all pipe ends

            foreach (var attr in Retriever.GetAllAttributes<DataSourceAttribute>())
            {
                var id = TypeInfo.Get(attr.TargetMember.DeclaringType).Id;
                if (!s_pipeEnds.TryGetValue(id, out var record))
                {
                    s_pipeEnds[id] = record = (new(), new());
                }

                record.sources.Add(attr);
            }

            foreach (var attr in Retriever.GetAllAttributes<DataTargetAttribute>())
            {
                var id = TypeInfo.Get(attr.TargetMember.DeclaringType).Id;
                if (!s_pipeEnds.TryGetValue(id, out var record))
                {
                    s_pipeEnds[id] = record = (new(), new());
                }

                if (!record.targets.TryGetValue(attr.DataName, out var attrs))
                {
                    record.targets[attr.DataName] = attrs = new();
                }

                attrs.Add(attr);
            }

            s_miBindHelper = typeof(DataFlowRegistry).GetMethod(nameof(BindHelper), HelperFlags);
        }

        static Action<object, object> BindHelper<TFrom>(string dataName, MemberInfo sourceMember, Type targetType, bool isRefData)
        {
            var preGetter = default(Func<object, object>);
            var mainGetter = default(Func<object, TFrom>);
            if (isRefData)
            {
                var memberType = GetMemberType(sourceMember);
                preGetter = sourceMember.DeclaringType.GetMemberGetter<object>(sourceMember.Name);
                mainGetter = memberType.GetMemberGetter<TFrom>(nameof(RefData<int>.Value));
            }
            else
            {
                mainGetter = sourceMember.DeclaringType.GetMemberGetter<TFrom>(sourceMember.Name);
            }

            var setters = new List<(Func<object, object>, Action<object, TFrom>)>();

            for (var baseType = targetType; baseType != null && typeof(IWatchable).IsAssignableFrom(baseType); baseType = baseType.BaseType)
            {
                var id = TypeInfo.Get(baseType).Id;
                if (s_pipeEnds.TryGetValue(id, out var record))
                {
                    if (!record.targets.TryGetValue(dataName, out var attrs)) continue;

                    foreach (var attr in attrs)
                    {
                        var targetMember = attr.TargetMember;
                        var memberType = GetMemberType(targetMember);
                        if (GetRefDataValueType(memberType) != null)
                        {
                            var memGetter = targetMember.DeclaringType.GetMemberGetter<object>(targetMember.Name);
                            var valSetter = memberType.GetMemberSetter<TFrom>(nameof(RefData<int>.Value));
                            setters.Add((memGetter, valSetter));
                        }
                        else
                        {
                            var valSetter = targetMember.DeclaringType.GetMemberSetter<TFrom>(targetMember.Name);
                            setters.Add((null, valSetter));
                        }
                    }
                }
            }

            if (preGetter != null)
            {
                return (source, target) =>
                {
                    source = preGetter(source);
                    var value = mainGetter(source);

                    foreach (var (memGetter, valSetter) in setters)
                    {
                        if (memGetter != null)
                        {
                            valSetter.Invoke(memGetter(target), value);
                        }
                        else
                        {
                            valSetter.Invoke(target, value);
                        }
                    }
                };
            }
            else
            {
                return (source, target) =>
                {
                    var value = mainGetter(source);

                    foreach (var (memGetter, valSetter) in setters)
                    {
                        if (memGetter != null)
                        {
                            valSetter.Invoke(memGetter(target), value);
                        }
                        else
                        {
                            valSetter.Invoke(target, value);
                        }
                    }
                };
            }
        }

        static Action<object, object> GetBinder(string dataName, MemberInfo sourceMember, Type targetType)
        {
            if (!s_flows.TryGetValue(sourceMember, out var binderLut))
            {
                s_flows[sourceMember] = binderLut = new();
            }

            var id = TypeInfo.Get(targetType).Id;
            if (!binderLut.TryGetValue(id, out var binder))
            {
                var sourceType = GetMemberType(sourceMember);
                var refValueType = GetRefDataValueType(sourceType);
                if (refValueType != null)
                {
                    sourceType = refValueType;
                }

                var miBinderInstance = s_miBindHelper.MakeGenericMethod(sourceType);
                binderLut[id] = binder = miBinderInstance.Invoke(null, new object[] { dataName, sourceMember, targetType, refValueType != null }) as Action<object, object>;
            }

            return binder;
        }

        static Type GetRefDataValueType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RefData<>))
            {
                return type.GenericTypeArguments[0];
            }
            else
            {
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Type GetMemberType(MemberInfo member)
        {
            if (member is FieldInfo field) return field.FieldType;
            if (member is PropertyInfo property) return property.PropertyType;
            throw null;
        }

        public static bool HasPipeEnds(Type type)
        {
            for (var baseType = type; baseType != null && typeof(IWatchable).IsAssignableFrom(baseType); baseType = baseType.BaseType)
            {
                var id = TypeInfo.Get(baseType).Id;
                if (s_pipeEnds.ContainsKey(id))
                {
                    return true;
                }
            }

            return false;
        }

        public static void BindAll(IEnumerable<IWatchable> modules, IScopeLifeKeeper lifeKeeper, ICollection<WatchScope> results)
        {
            using var _0 = CollectionPool.Get<List<IDisposable>>(out var disposables);
            using var _1 = CollectionPool.Get<Dictionary<string, (MemberInfo sourceMember, IWatchable sourceModule)>>(out var sources);
            using var _2 = CollectionPool.Get<Dictionary<string, Dictionary<Type, List<IWatchable>>>>(out var targets);

            foreach (var module in modules)
            {
                if (module == null) continue;
                if (module is UnityEngine.Object obj && !obj) continue;

                var type = module.GetType();
                for (var baseType = type; baseType != null && typeof(IWatchable).IsAssignableFrom(baseType); baseType = baseType.BaseType)
                {
                    var idModule = TypeInfo.Get(baseType).Id;
                    if (s_pipeEnds.TryGetValue(idModule, out var record))
                    {
                        // introduce priority?
                        foreach (var src in record.sources)
                        {
                            if (sources.TryGetValue(src.DataName, out var sourceMember))
                            {
                                Logger.Warning($"more than one data source for `{src.DataName}`, take {sourceMember} but {src.TargetMember} in {src.TargetMember?.DeclaringType}");
                            }
                            else
                            {
                                sources.Add(src.DataName, (src.TargetMember, module));
                            }
                        }

                        foreach (var (name, attrs) in record.targets)
                        {
                            if (!targets.TryGetValue(name, out var lutTargets))
                            {
                                var handle = CollectionPool.Get(out lutTargets);
                                disposables.Add(handle);
                                targets[name] = lutTargets;
                            }

                            foreach (var attrTarget in attrs)
                            {
                                var targetModuleType = attrTarget.TargetMember.DeclaringType;
                                if (!lutTargets.TryGetValue(targetModuleType, out var targetModules))
                                {
                                    var handle = CollectionPool.Get(out targetModules);
                                    disposables.Add(handle);
                                    lutTargets[targetModuleType] = targetModules;
                                }

                                targetModules.Add(module);
                            }
                        }
                    }
                }
            }


            foreach (var (name, (sourceMember, sourceModule)) in sources)
            {
                if (!targets.TryGetValue(name, out var lutTargets)) continue;

                foreach (var (type, targetModules) in lutTargets)
                {
                    var binder = GetBinder(name, sourceMember, type);
                    foreach (var targetModule in targetModules)
                    {
#warning TODO: switch to state binder
                        var scp = lifeKeeper.WatchEffect(() =>
                        {
                            try
                            {
                                binder(sourceModule, targetModule);
                            }
                            catch (Exception e)
                            {
                                Logger.Error(new Exception($"An error occur during data flow [{name}]({sourceModule.GetType().Name}.{sourceMember.Name}->{targetModule.GetType().Name})", e));
                            }
                        });
                        results.Add(scp);
                    }
                }
            }

            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }

    }
}
