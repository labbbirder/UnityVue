using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using com.bbbirder;
using UnityEngine.UIElements;
using System.Reflection;
using System;
using System.Runtime.CompilerServices;

[CustomPropertyDrawer(typeof(VisualWatchedAttribute))]
public class WatchedVisualizerEditor : PropertyDrawer
{
    Dictionary<string,bool> toggles = new();
    static FieldInfo s_LastRect;
    /// <summary>
    /// 在EditorLayout上下文分配指定的高度
    /// </summary>
    /// <param name="height"></param>
    /// <returns></returns>
    public static Rect applyRect(int depth,float height=18f) {
        s_LastRect??=typeof(EditorGUILayout).GetField("s_LastRect", BindingFlags.Static | BindingFlags.NonPublic);
        var rect = EditorGUILayout.GetControlRect(true, height);
        // rect.height += height;
        rect.x+=depth*8f;
        s_LastRect.SetValue(null, rect);
        return rect;
    }
    public void Parse(PropertyInfo prop,object obj,string path="",int depth=0){
        EditorGUI.BeginChangeCheck();
        try{
        object value = obj!=null?prop.GetValue(obj):null;
        // if(value is IWatched watched && watched.){
            Debug.Log($"{prop.PropertyType},{prop.PropertyType.IsSubclassOf(typeof(IList))}");
        // }
        if(value==null){
            EditorGUI.LabelField(applyRect(depth),prop.Name);
        }else if(prop.PropertyType.Equals(typeof(string))){
            // EditorGUILayout.GetControlRect(EditorGUILayout.s_)
            // EditorGUILayout.TextField(prop.Name,"");
            value = EditorGUI.TextField(applyRect(depth),prop.Name,(string)value);
        }else if(prop.PropertyType.Equals(typeof(int))){
            value = EditorGUI.IntField(applyRect(depth),prop.Name,(int)value);
        }else if(prop.PropertyType.Equals(typeof(long))){
            value = EditorGUI.LongField(applyRect(depth),prop.Name,(long)value);
        }else if(prop.PropertyType.Equals(typeof(float))){
            value = EditorGUI.FloatField(applyRect(depth),prop.Name,(float)value);
        }else if(prop.PropertyType.Equals(typeof(double))){
            value = EditorGUI.DoubleField(applyRect(depth),prop.Name,(double)value);
        }else if(prop.PropertyType.Equals(typeof(bool))){
            value = EditorGUI.Toggle(applyRect(depth),prop.Name,(bool)value);
        }else if(prop.PropertyType.GetInterface(nameof(IList))!=null){
            EditorGUI.LabelField(applyRect(depth),prop.Name,"array");
        }else if(prop.Name=="__rawObject"||prop.Name=="onGetProperty"||prop.Name=="onSetProperty"){
            return;
        }else{
            var hasChild = false;
            var newpath = path+"."+prop.Name;
            foreach(var subp in prop.PropertyType.GetProperties()){
                if(!hasChild){
                    hasChild = true;
                    toggles.TryAdd(newpath,false);
                    toggles[newpath] = EditorGUI.Foldout(applyRect(depth),toggles[newpath],prop.Name,true);// prop.Name,toggles[newpath]);
                    // Debug.Log($"{idx}:{toggles[idx]}");
                }
                if(toggles[newpath])Parse(subp,value,newpath,depth+1);
            }
            // if(hasChild){
            //     EditorGUILayout.EndFoldoutHeaderGroup();
            // }
            return;
        }
        }catch(Exception e){
            Debug.LogError($"Parse Error On:{prop.Name}\n{e.Message}");
        }

        if(EditorGUI.EndChangeCheck()){

        }
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var target = property.serializedObject.targetObject;
        var field = target.GetType().GetField(property.name);
        var value = field.GetValue(target);
        foreach(var prop in field.FieldType.GetProperties())
            Parse(prop,value,field.Name);
    }
}
