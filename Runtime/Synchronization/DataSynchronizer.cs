using System.Collections.Generic;

namespace BBBirder.UnityVue.Synchronization
{

    public interface ISynchronizable : IWatchable
    {
        int SyncId { get; set; }
    }

    public class DataSynchronizer
    {
        class ProxyRecord
        {
            public ISynchronizable proxy;
            public int refcount;
        }
        List<ProxyRecord> proxys = new();
        public void ApplyPropertySetCommand(SetPropertyCommand cmd, int id, object key, object value)
        {
            var proxy = proxys[id].proxy;
            var prevValue = proxy.RawGet(key);
            if (prevValue is ISynchronizable prevSync)
            {
                if (--proxys[prevSync.SyncId].refcount <= 0)
                {
                    // Remove Record at prevSync.SyncId
                }
            }
            if (value is ISynchronizable sync)
            {
                if (sync.SyncId == -1)
                {
                    // Create New Record, Add to End
                }
                else
                {
                    proxys[sync.SyncId].refcount++;
                }
            }
            proxy.RawSet(key, value);
        }

        public void CreateCommand(InsertListCommand cmd, int id, int index, object value)
        {
            var list = proxys[id].proxy as IWatchableList;
            if (list is null) return;
            list.Insert(index, value);
        }
        public void CreateCommand(InsertRemoveCommand cmd, int id, int index)
        {
            var list = proxys[id].proxy as IWatchableList;
            if (list is null) return;
            list.RemoveAt(index);
        }

        public void CreateCommand(CreateObjectCommand cmd, object value)
        {

        }
    }


    public enum SyncCommand : sbyte
    {
        InstanceCreation,
        PropertySet,
        ListInsert,
        ListRemove,
        CollectionClear,
        DictionaryAdd,
        DictionaryRemove,
        SetAdd,
        SetRemove,
    }


    public static class SynchronizeCommands
    {
        public readonly static SetPropertyCommand SetPropertyCommand = new();
        public readonly static InsertListCommand InsertListCommand = new();
        public readonly static InsertRemoveCommand InsertRemoveCommand = new();
        public readonly static CreateObjectCommand CreateObjectCommand = new();
    }

    public abstract class CommandBase
    {

    }

    public class SetPropertyCommand : CommandBase
    {

    }

    public class InsertListCommand : CommandBase
    {

    }

    public class InsertRemoveCommand : CommandBase
    {

    }

    public class CreateObjectCommand : CommandBase
    {

    }
}
