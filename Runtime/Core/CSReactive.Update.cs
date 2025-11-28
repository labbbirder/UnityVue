using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BBBirder.UnityVue
{
    public partial class CSReactive
    {
        internal const int DEFAULT_UPDATE_LIMIT = 100;

        private static long s_stackUpdateCount;
        private static long s_cleanUpdateCount;
        [ThreadStatic] private static bool s_shouldCollectReference;
        [ThreadStatic] internal static WatchScope s_executingScope;
        private static readonly HashSet<WatchScope> s_dirtyScopes = new();
        private static readonly ArrayPool<WatchScope> s_tempScopesPool = ArrayPool<WatchScope>.Create();
        private static readonly HashSet<WatchScope> s_exceededScopes = new();
        public static int UpdateLimit = DEFAULT_UPDATE_LIMIT;

        // TODO: replace key to generic type
        static ConcurrentQueue<(IWatchable watchable, object key)> shared_accessQueue = new();
        internal static void OnGlobalBeforeGetProperty(IWatchable watched, object key)
        {
#if !PLATFORM_WEBGL
            if (Thread.CurrentThread.ManagedThreadId != 1)
            {
                // shared_accessQueue.Enqueue((false, s_executingScope, watched, key));
                // Logger.Warning($"Reject to access {key} on {watched} in non-main thread.");
                return;
            }
#endif

            if (!s_shouldCollectReference)
            {
                return;
            }

            /** UPDATE: watchable will be watched by default, so we dont need it anymore. **/
            //             if (watched.IsPropertyWatchable(key))
            //             {
            //                 if (watched.RawGet(key) is IWatchable propertyValue)
            //                     MakeProxy(propertyValue);
            //             }

            if (s_executingScope == null)
            {
                return;
            }

            RegisterScopeDependency(s_executingScope, watched, key);
        }

        internal static void OnGlobalAfterSetProperty(IWatchable watched, object key)
        {
#if !PLATFORM_WEBGL
            if (Thread.CurrentThread.ManagedThreadId != 1)
            {
                shared_accessQueue.Enqueue((watched, key));
                // Logger.Warning($"Reject to access {key} on {watched} in non-main thread.");
                return;
            }
#endif

            var payload = watched.Payload;
            if (payload != null && payload.Scopes.TryGetValue(key, out var collection))
            {
                var count = collection.Count;
                var tempScopes = s_tempScopesPool.Rent(count);

                try
                {
                    collection.CopyTo(tempScopes);

                    // Flush all scopes
                    for (int i = 0; i < count; i++)
                    {
                        var scp = tempScopes[i];
                        if (scp.flushMode == ScopeFlushMode.Immediate)
                        {
                            if (!scp.IsDisposed)
                            {
                                /** Unity IL2CPP icall of RuntimeHelpers::SufficientExecutionStack always returns true **/

                                // try
                                // {
                                //     RuntimeHelpers.EnsureSufficientExecutionStack();
                                // }
                                // catch { }

                                Interlocked.Increment(ref s_stackUpdateCount);
                                RunScope(scp);
                            }
                        }
                        else if (scp.flushMode == ScopeFlushMode.PostUpdate)
                        {
                            if (!scp.IsDisposed)
                            {
                                SetDirty(scp);
                            }
                        }
                        else
                        {
                            throw new NotImplementedException("Unreachable Assertion!");
                        }
                    }
                }
                finally
                {
                    s_tempScopesPool.Return(tempScopes, true);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RegisterScopeDependency(WatchScope scope, IWatchable watchable, object key)
        {
            var payload = watchable.Payload;

            if (!payload.Scopes.TryGetValue(key, out var collection))
            {
                payload.Scopes[key] = collection = new();
                collection.accessSource = (watchable, key);
            }

            collection.Add(scope);
            scope.includedTables.Add(collection);
        }

        /** UPDATE: watchable will be watched by default, so we dont need it anymore. **/
        // internal static T MakeProxy<T>(T watched) where T : IWatchable
        // {
        //     var payload = watched.Payload;
        //     if ((payload.StatusFlags & (byte)PreservedWatchableFlags.Reactive) == 0)
        //     {
        //         payload.onBeforeGet -= OnGlobalBeforeGetProperty;
        //         payload.onAfterSet -= OnGlobalAfterSetProperty;
        //         payload.onBeforeGet += OnGlobalBeforeGetProperty;
        //         payload.onAfterSet += OnGlobalAfterSetProperty;
        //         payload.StatusFlags |= (byte)PreservedWatchableFlags.Reactive;
        //     }

        //     return watched;
        // }

        internal static void RunScope(WatchScope scope, bool invokeNormalEffect = true, bool checkEnable = true)
        {
            scope.isDirty = false;

            if (scope.lifeKeeper != null && !scope.lifeKeeper.IsAlive)
            {
                ClearScopeDependencies(scope);
                return;
            }

            if (Interlocked.Read(ref s_stackUpdateCount) >= UpdateLimit)
            {
                scope.isDirty = true;
                RaiseUpdateCountExceedsLimitWarning(scope);
                s_exceededScopes.Add(scope);
                return;
            }

            using (ActiveScopeRegion.Create(scope))
            {
                ClearScopeDependencies(scope);

                var prevCollectState = s_shouldCollectReference;
                try
                {
                    s_shouldCollectReference = true;
                    AssertNotNull(scope.checker);

                    if (checkEnable && !scope.IsEnabled)
                    {
                        // scope.isDirty = true;
                        return;
                    }

                    scope.checker();
                }
                catch (Exception e)
                {
                    if (e is StackOverflowException)
                    {
                        throw;
                    }

                    Logger.Error(e);
                }
                finally
                {
                    s_shouldCollectReference = prevCollectState;
                }

                if (invokeNormalEffect)
                {
                    try
                    {
                        scope.normalEffect?.Invoke();
                    }
                    catch (Exception e)
                    {
                        if (e is StackOverflowException)
                        {
                            throw;
                        }

                        Logger.Error(e);
                    }
                }
            }
        }

        private static void RaiseUpdateCountExceedsLimitWarning(WatchScope scope)
        {
#if ENABLE_UNITY_VUE_TRACKER
            var frame = scope.stackFrames
                .Where(f => f.GetMethod().GetCustomAttribute<DebuggerHiddenAttribute>() == null)
                .FirstOrDefault();
            Logger.Warning("effect times exceed max iter count " + frame?.GetFileName() + ":" + frame?.GetFileLineNumber());
#else
            Logger.Warning("effect times exceed max iter count");

#endif
        }

        /// <summary>
        /// Update and clear all dirty scopes.
        /// </summary>
        /// <remarks>
        /// Usually it's unnecessary to do this manually. The system will clear the dirties automatically at a proper time.
        /// </remarks>
        public static void UpdateDirtyScopes()
        {
            if (Thread.CurrentThread.ManagedThreadId != 1)
            {
                Logger.Warning("UpdateDirtyScopes can only be called from main thread.");
                return;
            }

            Interlocked.Exchange(ref s_cleanUpdateCount, 0);

            while (shared_accessQueue.TryDequeue(out var result))
            {
                var (watched, key) = result;

                OnGlobalAfterSetProperty(watched, key);
            }

            while (s_dirtyScopes.Count > 0)
            {
                if (Interlocked.Increment(ref s_cleanUpdateCount) >= UpdateLimit)
                {
                    RaiseUpdateCountExceedsLimitWarning(s_dirtyScopes.First());
                    s_dirtyScopes.Clear();
                    break;
                }

                using (CollectionPool.Get<List<WatchScope>>(out var scopes))
                {
                    foreach (var scp in s_dirtyScopes)
                    {
                        scopes.Add(scp);
                    }

                    foreach (var dirtyScope in scopes)
                    {
                        RunScope(dirtyScope);
                        if (!dirtyScope.isDirty || s_exceededScopes.Contains(dirtyScope))
                        {
                            s_dirtyScopes.Remove(dirtyScope);
                        }
                    }
                }
            }

            s_exceededScopes.Clear();
        }

        internal static void SetDirty(WatchScope scope, bool dirty = true)
        {
            scope.isDirty = dirty;
            if (scope.autoClearDirty)
            {
                if (dirty)
                {
                    s_dirtyScopes.Add(scope);
                }
                else
                {
                    s_dirtyScopes.Remove(scope);
                }
            }
        }

        static void ClearScopeDependencies(WatchScope scope)
        {
            foreach (var collection in scope.includedTables)
            {
                collection.Remove(scope);
            }

            scope.includedTables.Clear();
        }

        internal static void FreeScope(WatchScope scope, bool removeCallbackInLifeKeeper)
        {
            if (Thread.CurrentThread.ManagedThreadId != 1)
            {
                Logger.Warning("WatchScope can only be disposed from main thread.");
                return;
            }

            ClearScopeDependencies(scope);
            if (removeCallbackInLifeKeeper && scope.lifeKeeper != null)
            {
                scope.lifeKeeper.Scopes.Remove(scope);
                scope.lifeKeeper = null;
            }

            scope.isDirty = false;
            s_dirtyScopes.Remove(scope);

            scope.checker = null;
            scope.normalEffect = null;
            scope.onDisposed?.Invoke();
            scope.onDisposed = null;
        }

        [Conditional("DEBUG")]
        static void AssertNotNull(object obj)
        {
            if (obj is null)
            {
                throw new($"Argument must not be null.");
            }
        }

        public static SuppressCollectRegion BeginSuppressCollectRegion()
        {
            return SuppressCollectRegion.Create();
        }

        public ref struct SuppressCollectRegion
        {
            private bool prev;

            internal static SuppressCollectRegion Create()
            {
                var region = new SuppressCollectRegion()
                {
                    prev = s_shouldCollectReference,
                };
                s_shouldCollectReference = false;
                return region;
            }

            public void Dispose()
            {
                s_shouldCollectReference = prev;
            }
        }

        private ref struct ActiveScopeRegion
        {
            private WatchScope prev;
            private WatchScope current;

            public static ActiveScopeRegion Create(WatchScope scope)
            {
                var region = new ActiveScopeRegion()
                {
                    prev = s_executingScope,
                    current = scope,
                };
                s_executingScope = scope;
                return region;
            }

            public void Dispose()
            {
                if (current != s_executingScope)
                {
                    throw new("stacking scopes not poped in a correct order, are you access this via multi-thread?");
                }

                s_executingScope = prev;
                if (prev == null)
                {
                    Interlocked.Exchange(ref s_stackUpdateCount, 0);
                }
            }
        }
    }
}
