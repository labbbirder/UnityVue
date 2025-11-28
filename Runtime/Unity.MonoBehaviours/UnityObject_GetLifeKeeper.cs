using System;
using System.Collections.Generic;
using UnityEngine;

namespace BBBirder.UnityVue
{
    public static class UnityExtensions
    {
        static Dictionary<UnityEngine.Object, BehaviourLifeKeeper> s_cancellationRegisteredUnityObject = new();
        static Action<object> s_releaseByCancellation;

        static UnityExtensions()
        {
            s_releaseByCancellation = ReleaseByCancellation;
        }

        static void ReleaseByCancellation(object state)
        {
            var behaviour = state as MonoBehaviour;

            if (s_cancellationRegisteredUnityObject.TryGetValue(behaviour, out var lifeKeeper))
            {
                lifeKeeper.Release();
                s_cancellationRegisteredUnityObject.Remove(behaviour);
            }
        }

        /// <summary>
        /// The a proper LifeKeeper for this Unity Object.
        /// </summary>
        /// <param name="unityObject"></param>
        /// <param name="ignoreEnableState">Disabled Object can still receive updates when it set to True.</param>
        /// <returns></returns>
        public static IScopeLifeKeeper GetLifeKeeper(this UnityEngine.Object unityObject)
        {
            if (unityObject is IScopeLifeKeeper lifeKeeper) return lifeKeeper;

            if (unityObject is MonoBehaviour behaviour)
            {
                if (!s_cancellationRegisteredUnityObject.TryGetValue(behaviour, out var behaviourLifeKeeper))
                {
                    var cancellationToken = behaviour.destroyCancellationToken;
                    behaviourLifeKeeper = new BehaviourLifeKeeper()
                    {
                        behaviour = behaviour,
                    };
                    cancellationToken.Register(s_releaseByCancellation, behaviour);
                    s_cancellationRegisteredUnityObject[behaviour] = behaviourLifeKeeper;
                }

                return behaviourLifeKeeper;
            }

            if (unityObject is GameObject go)
            {
                var trigger = go.GetComponent<DestroyTrigger>();
                if (!trigger) trigger = go.AddComponent<DestroyTrigger>();

                return trigger;
            }

            if (unityObject is Transform trans)
            {
                var trigger = trans.GetComponent<DestroyTrigger>();
                if (!trigger) trigger = trans.gameObject.AddComponent<DestroyTrigger>();

                return trigger;
            }

            if (!UnityVueDriver.TryGetPollingLifeKeeper(unityObject, out var pollingLifeKeeper))
            {
                pollingLifeKeeper = new PollingLifeKeeper()
                {
                    lifeDetector = () => unityObject,
                    enableDetector = unityObject is Behaviour hehav
                        ? () => hehav.isActiveAndEnabled
                        : () => true,
                };
                UnityVueDriver.RegisterPollingLifeKeeper(unityObject, pollingLifeKeeper);
            }

            return pollingLifeKeeper;
        }

    }

    internal class BehaviourLifeKeeper : IScopeLifeKeeper
    {
        public MonoBehaviour behaviour;
        public SimpleList<WatchScope> Scopes { get; } = new();
        public bool IsAlive => behaviour;
        public bool IsEnabled => true;

        internal void Release()
        {
            this.ReleaseScopes();
            behaviour = default;
        }
    }

    public class PollingLifeKeeper : IScopeLifeKeeper
    {
        public Func<bool> lifeDetector;
        public Func<bool> enableDetector;
        public SimpleList<WatchScope> Scopes { get; } = new();
        public bool IsAlive => lifeDetector();
        public bool IsEnabled => true;

        internal void Release()
        {
            this.ReleaseScopes();
            lifeDetector = null;
        }
    }
}
