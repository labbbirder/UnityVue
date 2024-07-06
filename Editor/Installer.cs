using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BBBirder.UnityEditor
{
    internal class Installer : RoslynUpdater
    {
        static string SourceDllPath =
            Path.Join(PackageUtils.GetPackagePath(), "VSProj~/UnityVue.SG.dll");
        static Installer m_Instance;
        static Installer Instance => m_Instance ??= new();
        static bool entered = false;

        private Installer() : base(SourceDllPath) { }

        [InitializeOnLoadMethod]
        static void Setup()
        {
            bool isFocusPrev = false;
            EditorApplication.update += () =>
            {
                var isFocus = InternalEditorUtility.isApplicationActive;
                if (!isFocusPrev && isFocus) OnEditorFocus();
                isFocusPrev = isFocus;
            };

            OnEditorFocus();
        }

        static void OnEditorFocus()
        {
            //not reenterable
            if (entered) return;
            entered = Instance.RunWorkFlow();

            if (File.Exists(SourceDllPath))
            {
                entered = false;
            }
        }
    }

}
