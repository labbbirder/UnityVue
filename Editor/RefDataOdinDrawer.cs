#if ODIN_INSPECTOR

using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace BBBirder.UnityVue.Editor
{
    public class RefDataOdinDrawer<T> : OdinValueDrawer<RefData<T>>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            Property.Children[nameof(RefData<int>.Value)].Draw(label);
        }
    }
}

#endif
