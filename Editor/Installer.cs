using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

internal partial class Installer
{
    const string ExportPath = "Assets/Plugins/com.bbbirder.csreactive";
    static string AssetGUID = "f45c27409355d4b40891fc219f340827";
    [MenuItem("Tools/bbbirder/Install CSReactive")]    
    static void InstallUnityPackage()
    {
        if(!Directory.Exists(ExportPath)){
            var assetPath = AssetDatabase.GUIDToAssetPath(AssetGUID);
            AssetDatabase.ImportPackage(assetPath,false);
        }
    }

}

partial class Installer:AssetPostprocessor{
    const bool AutoInstall = true;
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
        if(AutoInstall) InstallUnityPackage();
    }
}