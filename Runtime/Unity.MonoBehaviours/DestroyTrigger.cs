using UnityEngine;

namespace BBBirder.UnityVue
{
    internal class DestroyTrigger : MonoBehaviour, IScopeLifeKeeper
    {
        public bool IsAlive => !!this;
        private RefData<bool> _isEnabled = new(false);
        public virtual bool IsEnabled => InternalEnabledState;

        protected internal bool InternalEnabledState
        {
            get => _isEnabled;
            private set => _isEnabled.Value = value;
        }

        public SimpleList<WatchScope> Scopes { get; } = new();

        protected virtual void OnEnable()
        {
            InternalEnabledState = true;
        }

        protected virtual void OnDisable()
        {
            InternalEnabledState = false;
        }
    }
}
