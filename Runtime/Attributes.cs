using System;

namespace com.bbbirder{
    public class WatchableAttribute:Attribute{
        public DataFlags flags;
        public WatchableAttribute(DataFlags flags = DataFlags.Default){
            this.flags = flags;
        }
    }
    // public class NonNestedAttribute:Attribute{

    // }
    // public class NestedAttribute:Attribute{
        
    // }
    
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
        // /// <summary>
        // /// whether write to the raw object when changed
        // /// </summary>
        // DontWriteBack = 1<<3,
    }
}