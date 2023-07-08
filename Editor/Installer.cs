using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace com.bbbirder.unityeditor{
    internal class Installer:RoslynUpdater
    {
        static string SourceDllPath =
            Path.Join(PackageUtils.GetPackagePath(),"VSProj~/OnChange.sg.dll");
        static Installer m_Instance;
        static Installer Instance => m_Instance ??= new();
        static bool entered = false;
        private Installer():base(SourceDllPath)
        {

        }
        [InitializeOnLoadMethod]
        static void Setup()
        {
            bool isFocus = InternalEditorUtility.isApplicationActive;
            EditorApplication.update += ()=>{
                var cur = InternalEditorUtility.isApplicationActive;
                if(!isFocus && cur)  OnEditorFocus();
                isFocus = cur;
            };

            OnEditorFocus();
        }
        
        static void OnEditorFocus(){
            //not reenterable
            if(entered) return;
            entered = Instance.RunWorkFlow();
        }
    }

}
