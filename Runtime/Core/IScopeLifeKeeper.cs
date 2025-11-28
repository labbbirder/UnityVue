namespace BBBirder.UnityVue
{
    public interface IScopeLifeKeeper
    {
        bool IsAlive { get; }
        /// <summary>
        /// NOTE: Remember to call `OnEnteredEnableState()` after state set to `true`
        /// </summary>
        bool IsEnabled { get; }

        SimpleList<WatchScope> Scopes { get; }
    }

}
