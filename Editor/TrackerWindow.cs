using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build;
using System.Linq;
using System;
using System.Diagnostics;
using Unity.CodeEditor;
using System.Reflection;

namespace BBBirder.UnityVue.Editor
{
    class ItemData
    {
        public string file;
        public int ln, cn;
        public string stacks;
    }

    public class TrackerWindow : EditorWindow
    {
        [SerializeField] VisualTreeAsset rootUIAsset;
        [SerializeField] VisualTreeAsset elementUIAsset;

        [MenuItem("Tools/bbbirder/Unity Vue Tracker")]
        public static void ShowWindow()
        {
            var window = GetWindow<TrackerWindow>();
            window.titleContent = new GUIContent("Unity Vue Tracker");
            window.Show();
        }
        static void AlterScriptingDefine(string symbolName, bool enable)
        {
            NamedBuildTarget activeBuildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            var defines = PlayerSettings.GetScriptingDefineSymbols(activeBuildTarget)
                .Split(';').ToList();
            var beforeCount = defines.Count;
            if (enable)
            {
                if (!defines.Contains(symbolName))
                    defines.Add(symbolName);
            }
            else
            {
                if (defines.Contains(symbolName))
                    defines.Remove(symbolName);
            }

            if (beforeCount != defines.Count)
            {
                PlayerSettings.SetScriptingDefineSymbols(activeBuildTarget, string.Join(';', defines));
            }
        }


        void CreateGUI()
        {
            var globalData = GlobalTrackerData.Instance;
            rootUIAsset.CloneTree(rootVisualElement);
            var togEnable = rootVisualElement.Q<Toggle>("togEnable");
            var lstScopes = rootVisualElement.Q<ListView>("scopes");
            var txtTotal = rootVisualElement.Q<Label>("txtTotal");
            var txtDirty = rootVisualElement.Q<Label>("txtDirty");
            var btnRefresh = rootVisualElement.Q<Button>("btnRefresh");
            var btnCollect = rootVisualElement.Q<Button>("btnCollect");
            var togAutoRefresh = rootVisualElement.Q<Toggle>("togAutoRefresh");

            togEnable.value = globalData.Enabled;
            togEnable.RegisterValueChangedCallback(e =>
            {
                AlterScriptingDefine("ENABLE_UNITY_VUE_TRACKER", e.newValue);
            });
#if ENABLE_UNITY_VUE_TRACKER
            lstScopes.makeItem = () =>
            {
                var ele = elementUIAsset.CloneTree();
                var fldTitle = ele.Q<Foldout>("fldTitle");
                var timeStamp = .0;
                fldTitle.RegisterCallback<ClickEvent>(e =>
                {
                    const double DOUBLE_CLICK_INTERVAL = 0.2;
                    var ele = e.currentTarget as Foldout;
                    var now = EditorApplication.timeSinceStartup;
                    if (ele.userData is ValueTuple<string, int, int> location)
                    {
                        var (file, ln, cn) = location;
                        if (now - timeStamp < DOUBLE_CLICK_INTERVAL)
                        {
                            CodeEditor.Editor.CurrentCodeEditor.OpenProject(file, ln, cn);
                        }
                    }
                    timeStamp = now;
                });
                return ele;
            };
            lstScopes.bindItem = (ele, i) =>
            {
                var item = lstScopes.itemsSource[i] as WeakReference<WatchScope>;
                var Foldout = ele.Q<Foldout>("fldTitle");

                if (!item.TryGetTarget(out var target) || target is null)
                {
                    Foldout.text = "<collected>";
                    Foldout.style.color = Color.red;
                    return;
                }
                Foldout.style.color = target.isDirty ? Color.white : Color.gray;
                Foldout.userData = null;
                var frame = target.stackFrames
                    .Where(f => f.GetMethod().GetCustomAttribute<DebuggerHiddenAttribute>() == null)
                    .FirstOrDefault();
                if (frame == null)
                {
                    Foldout.text = "no stacks";
                }
                else
                {
                    var file = frame.GetFileName();
                    var ln = frame.GetFileLineNumber();
                    var cn = frame.GetFileColumnNumber();
                    Foldout.text = (target.debugName ?? frame.GetMethod().Name)
                        + "\t" + file + ":" + ln;
                    // Foldout.Q<Label>("txtStacks").text = "<a href=\"Assets/Scripts/Attachment.cs\" line=12>sad</a>";
                    Foldout.userData = (file, ln, cn);
                }
                Foldout.Q<Label>("txtStacks").text =
                    string.Join('\n', target.stackFrames.Select(f => $"in {f.GetMethod()} ({f.GetFileName()}:{f.GetFileLineNumber()})"));
            };

            lstScopes.itemsSource = globalData.scopes;

            rootVisualElement.schedule.Execute(() =>
            {
                if (togAutoRefresh.value) UpdateUI();
            }).Every(2_000);

            btnRefresh.clicked += UpdateUI;
            btnCollect.clicked += GC.Collect;

            void UpdateUI()
            {
                RefreshGlobalTrackerData();
                lstScopes.RefreshItems();
                txtDirty.text = globalData.scopes.Count(scp => scp.TryGetTarget(out var target) && target.isDirty).ToString();
                txtTotal.text = globalData.scopes.Count.ToString();
            }

            static void RefreshGlobalTrackerData()
            {
                var scopes = GlobalTrackerData.Instance.scopes;
                for (int i = scopes.Count - 1; i >= 0; i--)
                {
                    var reference = scopes[i];
                    // remove those collected by gc
                    if (!reference.TryGetTarget(out var target) || target is null)
                    {
                        scopes.RemoveAt(i);
                        continue;
                    }
                    // remove the hiddens
                    if (target.hideInTracker)
                    {
                        scopes.RemoveAt(i);
                        continue;
                    }
                    // remove those completely disconnected from data
                    if (!target.isDirty && target.includedTables.Count == 0)
                    {
                        scopes.RemoveAt(i);
                        continue;
                    }
                }
            }

#endif
        }
    }
}
