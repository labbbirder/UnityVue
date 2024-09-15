#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace BBBirder.UnityVue
{
    [CustomEditor(typeof(ComponentBinder))]
    public class DataBinderEditor : Editor
    {
        ComponentBinder binder => target as ComponentBinder;
        const string ROOT_UI_GUID = "eb06b019921e7884dbfdeb06e17b60f5";
        const string ELEMENT_UI_GUID = "67a9fedd041e41c48a972331b10b7c21";
        // [SerializeField] VisualTreeAsset uiAssetRoot;
        // [SerializeField] VisualTreeAsset uiAssetElement;
        public override VisualElement CreateInspectorGUI()
        {
            binder.expressions ??= new();
            var uiAssetRoot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(ROOT_UI_GUID));
            var uiAssetElement = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(ELEMENT_UI_GUID));
            var uiRoot = uiAssetRoot.CloneTree();
            var elements = uiRoot.Q("elements");
            var btnAdd = uiRoot.Q<Button>("btnAdd");
            var btnSave = uiRoot.Q<Button>("btnSave");
            var expressionProp = serializedObject.FindProperty(nameof(binder.expressions));

            btnAdd.clicked += () =>
            {
                expressionProp.InsertArrayElementAtIndex(expressionProp.arraySize);
                serializedObject.ApplyModifiedProperties();
                var serializedProperty = expressionProp.GetArrayElementAtIndex(expressionProp.arraySize - 1);
                var ele = CreateExpressionItemUI(serializedProperty);
                elements.Add(ele);
            };

            btnSave.visible = Application.isPlaying;
            btnSave.clicked += () =>
            {
                EditorUtility.SetDirty(binder);
                // InternalEditorUtility.SaveToSerializedFileAndForget()
            };

            for (var i = 0; i < expressionProp.arraySize; i++)
            {
                var ele = CreateExpressionItemUI(expressionProp.GetArrayElementAtIndex(i));
                elements.Add(ele);
            }

            return uiRoot;

            VisualElement CreateExpressionItemUI(SerializedProperty serializedProperty)
            {
                var ele = uiAssetElement.CloneTree();
                var tog = ele.Q<Toggle>("tog");
                var txtInput = ele.Q<TextField>("txtInput");
                var txtStatus = ele.Q<Label>("txtStatus");
                var btnDelete = ele.Q<Button>("btnDelete");
                ele.BindProperty(serializedProperty);
                ele.RegisterCallback<AttachToPanelEvent>(e =>
                {
                    ele.style.backgroundColor = elements.IndexOf(ele) % 2 == 0 ? new Color(0, 0, 0, 0.15f) : Color.clear;
                });

                txtInput.RegisterValueChangedCallback(e =>
                {
                    if (tog.value)
                    {
                        binder.StopExpression(e.previousValue);
                        binder.StartExpression(e.newValue);
                    }
                    var status = binder.CompileTest(e.newValue);
                    txtStatus.text = status;
                    txtStatus.style.display = status is null ? DisplayStyle.None : DisplayStyle.Flex;
                });

                tog.RegisterValueChangedCallback(e =>
                {
                    txtInput.SetEnabled(e.newValue);
                    if (e.newValue)
                    {
                        binder.StartExpression(txtInput.text);
                    }
                    else
                    {
                        binder.StopExpression(txtInput.text);
                    }
                });

                btnDelete.clicked += () =>
                {
                    var empty = string.IsNullOrWhiteSpace(txtInput.text);
                    if (!empty && !EditorUtility.DisplayDialog("Confirm", "Delete this expression?", "Yes"))
                        return;

                    expressionProp.DeleteArrayElementAtIndex(elements.IndexOf(ele));
                    ele.RemoveFromHierarchy();
                    serializedObject.ApplyModifiedProperties();
                };

                return ele;
            }
        }

    }
}
#endif
