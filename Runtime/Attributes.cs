using System;

namespace com.bbbirder{
    [AttributeUsage(AttributeTargets.Class)]
    public class WatchableAttribute:Attribute{
        public DataFlags flags;
        public WatchableAttribute(DataFlags flags = DataFlags.Default){
            this.flags = flags;
        }
    }
    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Field|AttributeTargets.Event)]
    public class NotWatched:Attribute{
        
    }
    public enum DataFlags{
        Default = 0,
        // /// <summary>
        // /// whether ignore nested members
        // /// </summary>
        // Flat = 1<<0,
        // /// <summary>
        // /// whether ignore members from basetype
        // /// </summary>
        // IgnoreBaseType = 1<<1,
        // /// <summary>
        // /// whether generate nonpublic members
        // /// </summary>
        // NonPublic = 1<<2,
    }
}