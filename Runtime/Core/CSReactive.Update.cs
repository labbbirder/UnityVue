using System;
using System.Buffers;
using System.Collections.Generic;
using UnityEngine;

namespace BBBirder.UnityVue
{
    internal struct LutKey : IEquatable<LutKey>
    {
        public IWatchable watched;
        public object key;
        public override int GetHashCode()
        {
            return watched.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj is not LutKey lutKey || obj is null)
            {
                return false;
            }
            return object.ReferenceEquals(watched, lutKey.watched)
                && object.Equals(key, lutKey.key);
        }
        public override string ToString()
        {
            return $"{watched} -> {key}";
        }

        public bool Equals(LutKey lutKey)
        {
            return object.ReferenceEquals(watched, lutKey.watched)
                && object.Equals(key, lutKey.key);
        }
    }

    public partial class CSReactive
    {
        // public static T cast<T>(object v)
        // {
        //     try
        //     {
        //         return (T)(Type.GetTypeCode(typeof(T)) switch
        //         {
        //             TypeCode.SByte => Convert.ToSByte(v),
        //             TypeCode.Int16 => Convert.ToInt16(v),
        //             TypeCode.Int32 => Convert.ToInt32(v),
        //             TypeCode.Int64 => Convert.ToInt64(v),

        //             TypeCode.Byte => Convert.ToByte(v),
        //             TypeCode.UInt16 => Convert.ToUInt16(v),
        //             TypeCode.UInt32 => Convert.ToUInt32(v),
        //             TypeCode.UInt64 => Convert.ToUInt64(v),

        //             TypeCode.Single => Convert.ToSingle(v),
        //             TypeCode.Double => Convert.ToDouble(v),
        //             TypeCode.Decimal => Convert.ToDecimal(v),

        //             TypeCode.Char => Convert.ToChar(v),
        //             TypeCode.String => Convert.ToString(v),

        //             TypeCode.Boolean => Convert.ToBoolean(v),
        //             TypeCode.DateTime => Convert.ToDateTime(v),

        //             _ => v
        //         });
        //     }
        //     catch (Exception e)
        //     {
        //         Debug.LogException(e);
        //         Debug.LogError($"cannot cast {v} to typpe {typeof(T)}");
        //         return default;
        //     }

        // }

        public struct DataAccess
        {
            public IWatchable watchable;
            public object propertyKey;
        }

        // #if UNITY_2023_3_OR_NEWER
        // See: https://issuetracker.unity3d.com/issues/crashes-on-garbagecollector-collectincremental-when-entering-the-play-mode
        // Implement(IL2CPP): https://unity.com/releases/editor/whats-new/2021.2.1
        // Fixed(IL2CPP): https://unity.com/releases/editor/beta/2023.1.0b11
        // internal static ConditionalWeakTable<IWatched,Dictionary<string,HashSet<WatchScope>>> dataDeps;
        // #endif
        private static Dictionary<LutKey, ScopeCollection> dataRegistry = new();
        private static int s_frameIndex;
        private static bool shouldCollectReference;
        private static Stack<WatchScope> stackingScopes = new();
        private static HashSet<WatchScope> dirtyScopes = new();
        // private static HashSet<WatchScope> pendingDirtyScopes = new();
        private static HashSet<LutKey> s_emptyCollectionKeys = new();
        public static DataAccess lastAccess = new();

        static void OnGlobalGet(IWatchable watched, object key)
        {
            // populate last-access object
            CSReactive.lastAccess.watchable = watched;
            CSReactive.lastAccess.propertyKey = key;

            if (!CSReactive.shouldCollectReference) return;

            if (watched.IsPropertyWatchable(key))
            {
                var propertyValue = watched.RawGet(key) as IWatchable;
                if (propertyValue != null) SetProxy(propertyValue);
            }

            if (stackingScopes.Count == 0) return;

            var topScope = stackingScopes.Peek();
            var lutKey = new LutKey()
            {
                watched = watched,
                key = key,
            };
            if (!dataRegistry.TryGetValue(lutKey, out var collection))
            {
                dataRegistry[lutKey] = collection = new();
            }
            collection.Add(topScope);
            topScope.includedTables.Add((lutKey, collection));
        }


        static void OnGlobalSet(IWatchable watched, object key)
        {
            var pkey = new LutKey()
            {
                watched = watched,
                key = key,
            };
            if (dataRegistry.TryGetValue(pkey, out var collection))
            {
                var count = collection.Count;
                var temp = ArrayPool<WatchScope>.Shared.Rent(count);
                collection.CopyTo(temp);

                for (int i = 0; i < count; i++)
                {
                    var scp = temp[i];
                    if (scp.flushMode == ScopeFlushMode.Immediate)
                    {
                        RunScope(scp);
                    }
                    else if (scp.flushMode == ScopeFlushMode.LateUpdate)
                    {
                        SetDirty(scp);
                    }
                }
                ArrayPool<WatchScope>.Shared.Return(temp, true);
            }
        }

        static public T SetProxy<T>(T watched) where T : IWatchable
        {
            if (!watched.IsProxyInited)
            {
                watched.onPropertyGet += key => OnGlobalGet(watched, key);
                watched.onPropertySet += key => OnGlobalSet(watched, key);
                watched.IsProxyInited = true;
            }
            return watched;
        }

        #region Scope Management

        internal static void RunScope(WatchScope scope, bool invokeNormalEffect = true)
        {
            scope.isDirty = false;
            if (scope.lifeKeeper != null && !scope.lifeKeeper.IsAlive)
            {
                ClearScopeDependencies(scope);
                return;
            }
            if (scope.frameIndex != s_frameIndex)
            {
                scope.updatedInOneFrame = 0;
                scope.frameIndex = s_frameIndex;
            }
            if (++scope.updatedInOneFrame > scope.updateLimit)
            {
                scope.isDirty = true;
                Logger.Warning("effect times exceed max iter count");
                return;
            }

            using (ActiveScopeRegion.Create(scope))
            {
                ClearScopeDependencies(scope);
                using (EnableReferenceCollectRegion.Create())
                {
                    try
                    {
                        scope.effect();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }

                try
                {
                    if (invokeNormalEffect) scope.normalEffect?.Invoke();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        public static void UpdateDirtyScopes()
        {
            s_frameIndex++;

            while (dirtyScopes.Count > 0)
            {
                WatchScope dirtyScope = null;
                foreach (var scp in dirtyScopes)
                {
                    dirtyScope = scp;
                    break;
                }

                RunScope(dirtyScope);
                if (IsScopeClean(dirtyScope) || IsScopeUpdatedTooMuchTimes(dirtyScope))
                {
                    dirtyScopes.Remove(dirtyScope);
                }
            }

            // while (dirtyScopes.Count + pendingDirtyScopes.Count > 0)
            // {
            //     foreach (var scp in pendingDirtyScopes)
            //     {
            //         dirtyScopes.Add(scp);
            //     }
            //     pendingDirtyScopes.Clear();

            //     foreach (var scp in dirtyScopes)
            //     {
            //         RunScope(scp);
            //         if (IsScopeClean(scp) || IsScopeUpdatedTooMuchTimes(scp))
            //         {
            //             removedDirtyScopes.Add(scp);
            //         }
            //     }

            //     foreach (var scp in removedDirtyScopes)
            //     {
            //         dirtyScopes.Remove(scp);
            //     }
            //     removedDirtyScopes.Clear();
            // }

            foreach (var key in s_emptyCollectionKeys)
            {
                if (dataRegistry.TryGetValue(key, out var collection))
                {
                    if (collection.Count == 0)
                    {
                        dataRegistry.Remove(key);
                    }
                }
            }
        }

        internal static void SetDirty(WatchScope scope, bool dirty = true)
        {
            scope.isDirty = dirty;
            if (dirty)
            {
                CSReactive.dirtyScopes.Add(scope);
            }
            else
            {
                CSReactive.dirtyScopes.Remove(scope);
            }
        }

        static bool IsScopeUpdatedTooMuchTimes(WatchScope scope)
        {
            return scope.updatedInOneFrame > scope.updateLimit && scope.frameIndex == s_frameIndex;
        }

        static bool IsScopeClean(WatchScope scope)
        {
            return !scope.isDirty;
        }

        static void ClearScopeDependencies(WatchScope scope)
        {

            foreach (var (key, collection) in scope.includedTables)
            {
                collection.Remove(scope);
                if (collection.Count == 0)
                {
                    s_emptyCollectionKeys.Add(key);
                }
            }
            scope.includedTables.Clear();
        }

        internal static void FreeScope(WatchScope scope)
        {
            ClearScopeDependencies(scope);
            CSReactive.dirtyScopes.Remove(scope);
            // CSReactive.pendingDirtyScopes.Remove(scope);
        }

        #endregion // Scope Management

        private struct ActiveScopeRegion : IDisposable
        {
            private WatchScope scope;
            public static ActiveScopeRegion Create(WatchScope scope)
            {
                CSReactive.stackingScopes.Push(scope);
                return new()
                {
                    scope = scope,
                };
            }
            public void Dispose()
            {
                if (scope != CSReactive.stackingScopes.Pop())
                {
                    throw new("stacking scopes not poped in a correct order, are you access this via multi-thread?");
                }
            }
        }

        private struct EnableReferenceCollectRegion : IDisposable
        {
            public static EnableReferenceCollectRegion Create()
            {
                CSReactive.shouldCollectReference = true;
                return new();
            }
            public void Dispose()
            {
                CSReactive.shouldCollectReference = false;
            }
        }


    }
}
