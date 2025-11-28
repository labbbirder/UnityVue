namespace BBBirder.UnityVue
{
    public interface IValuedData<T> : IWatchable
    {
        T GetValue();
    }
}