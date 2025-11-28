#if ODIN_INSPECTOR

using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace BBBirder.UnityVue.Editor
{
    public class ComputedOdinDrawer<T> : OdinValueDrawer<Computed<T>>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var v = Property.ValueEntry.WeakSmartValue as Computed<T>;
            EditorGUILayout.LabelField(label?.text ?? "", v is null ? "" : v.Value.ToString());
        }
    }
}

#endif
