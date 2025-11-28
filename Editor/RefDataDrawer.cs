using UnityEditor;
using UnityEngine;

namespace BBBirder.UnityVue.Editor
{
    [CustomPropertyDrawer(typeof(RefData<>), true)]
    public class RefDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var innerProp = property.FindPropertyRelative("__rawObject");
            if (innerProp == null)
            {
                EditorGUI.LabelField(position, label);
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, innerProp, label);
            // f = EditorGUI.FloatField(position, f);
            if (EditorGUI.EndChangeCheck())
            {
                var refData = property.boxedValue as IWatchable;
                refData.Payload.onAfterSet?.Invoke(refData, "Value");
            }
        }
    }
}
