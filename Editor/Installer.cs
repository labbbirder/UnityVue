using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

internal class Installer
{
    static string AssetGUID = "f45c27409355d4b40891fc219f340827";
    [InitializeOnLoadMethod]    
    static void InstallUnityPackage()
    {
        if(!Directory.Exists("Assets/Plugins/com.bbbirder.csreactive")){
            Debug.Log("install package");
            var assetPath = AssetDatabase.GUIDToAssetPath(AssetGUID);
            AssetDatabase.ImportPackage(assetPath,true);
        }
    }
}
