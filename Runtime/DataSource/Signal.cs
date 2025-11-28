using System.Runtime.CompilerServices;

namespace BBBirder.UnityVue
{
    /// <summary>
    /// Used to listen and trigger an arbitrary state change.
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var stateChangeSignal = new Signal();
    ///
    /// WatchEffect(()=>{
    ///     signal.Detect();
    ///     Debug.Log("something happened");
    /// });
    ///
    /// stateChangeSignal.Trigger(); // outputs: "something happened"
    /// ]]>
    /// </code>
    /// </example>
    /// </summary>
    public class Signal
    {
        RefData<bool> inner = CSReactive.Ref(false);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void Detect()
        {
            _ = inner.Value;
        }

        public void Trigger()
        {
            inner.Value ^= true;
        }
    }
}
