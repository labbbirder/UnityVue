using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using System;
using UnityEngine.UIElements;
using com.bbbirder;
using System.Linq;
using System.Text;
namespace com.bbbirder.unity{
    #if UNITY_EDITOR
    [CustomEditor(typeof(ScopeVisualizer))]
    public class ScopeVisualizerEditor : Editor
    {
        class ElementUI{
            public Foldout fld;
            public ListView listDeps;
        }
        const string GUID_ROOT = "2e352f2f86c67b443b5ac32db1150436";
        const string GUID_ELEMENT = "ec18f4f9616153d47837baa252f0f43c";
        static StringBuilder sbuilder = new();
        public string BuildString(params object[] patterns){
            sbuilder.Clear();
            foreach(var pat in patterns) sbuilder.Append(pat);
            return sbuilder.ToString();
        }
        public override VisualElement CreateInspectorGUI()
        {
            var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(GUID_ROOT));
            var vta_ele = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(GUID_ELEMENT));
            var root = vta.CloneTree();
            var listView = root.Q<ListView>("list");
            var btnGC = root.Q<Button>("btnGC");
            var btnClean = root.Q<Button>("btnClean");
            btnGC.clicked+=()=>{
                GC.Collect();
            };
            btnClean.clicked+=()=>{
                ScopeVisualizer.Instance.refs.RemoveAll(r=>!r.TryGetTarget(out var _));
            };
            listView.makeItem+=()=>{
                var ele = vta_ele.CloneTree();
                var eui = new ElementUI{
                    fld = ele.Q<Foldout>("fld"),
                    listDeps = ele.Q<ListView>("listDeps"),
                };
                eui.listDeps.makeItem+=()=>new Label();
                eui.listDeps.bindItem+=(ele,i)=>{
                    var lbl = ele as Label;
                    lbl.text = eui.listDeps.itemsSource[i] as string;
                };
                ele.userData = eui;
                return ele;
            };
            listView.bindItem+=(ele,i)=>{
                var eui = ele.userData as ElementUI;
                var data = listView.itemsSource[i] as WeakReference<CSReactive.WatchScope>;
                var isValid = data.TryGetTarget(out var scope);
                eui.fld.style.color = isValid?Color.green:Color.red;
                // eui.fld.text = isValid?scope.name:$"[released]";
                if(isValid) eui.fld.text = scope.name;
                // eui.listDeps.itemsSource = isValid
                //     ?scope.deps.SelectMany(kvp=>kvp.Value.Select(name=>BuildString(kvp.Key.GetType().Name,":",name))).ToList()
                //     :new object[]{};
            };
            listView.itemsSource = ScopeVisualizer.Instance.refs;

            EditorApplication.update+=UpdateList;

            return root;
            void UpdateList(){
                if(root.panel==null){
                    EditorApplication.update-=UpdateList;
                    return;
                }
                listView.RefreshItems();

            }
        }
    }

    public class ScopeVisualizer:MonoBehaviour{
        
        static ScopeVisualizer m_Instance;
        public static ScopeVisualizer Instance{
            get {
                // var go = GameObject.Find(nameof(ScopeVisualizer));
                if(!m_Instance){
                    var go = new GameObject(nameof(ScopeVisualizer));
                    // go.hideFlags = HideFlags.DontSave;
                    m_Instance = go.AddComponent<ScopeVisualizer>();
                }
                return m_Instance;
            }
        }
        public List<WeakReference<CSReactive.WatchScope>> refs = new();
    }
    #endif
}
