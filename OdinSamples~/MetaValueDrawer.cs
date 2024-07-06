// using System;
// using System.Collections.Generic;
// using System.Reflection;
// using System.Linq;
// using Sirenix.OdinInspector;
// using Sirenix.OdinInspector.Editor;
// using UnityEngine;
// using Sirenix.Serialization;
// using Sirenix.Utilities.Editor;
// using UnityEditor;

// namespace com.bbbirder.unity.editor
// {
//     public class MetaValueProcessor : OdinAttributeProcessor<MetaToken>
//     {
//         public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
//         {
//             attributes.Add(new HideReferenceObjectPickerAttribute());
//         }
//     }

//     public class MetaValueDrawer : OdinValueDrawer<MetaValue>
//     {
//         enum PrimitiveType{
//             Null,String,
//             Int,Long,
//             Float,Double,
//             Bool,
//         }
//         protected override void DrawPropertyLayout(GUIContent label)
//         {
//             // CallNextDrawer(label);
//             Rect rect = EditorGUILayout.GetControlRect();
//             var metaValue = ValueEntry.SmartValue;
//             if(metaValue == null) return;

//             EditorGUI.BeginChangeCheck();
//             if(metaValue.value is null){
//                 MetaValue v = EditorGUI.EnumPopup(rect,PrimitiveType.Null) switch {
//                     PrimitiveType.String=>"",
//                     PrimitiveType.Int => 0,
//                     PrimitiveType.Float => 0F,
//                     PrimitiveType.Long => 0L,
//                     PrimitiveType.Double => 0D,
//                     PrimitiveType.Bool => false,
//                     _ => null,
//                 };
//                 metaValue = v;
//             }else if(metaValue.value is float f){
//                 metaValue = EditorGUI.FloatField(rect,f);
//             }else if(metaValue.value is double d){
//                 metaValue = EditorGUI.DoubleField(rect,d);
//             }else if(metaValue.value is int i){
//                 metaValue = EditorGUI.IntField(rect,i);
//             }else if(metaValue.value is long l){
//                 metaValue = EditorGUI.LongField(rect,l);
//             }else if(metaValue.value is string s){
//                 metaValue = EditorGUI.TextField(rect,s);
//             }else if(metaValue.value is bool b){
//                 metaValue = EditorGUI.Toggle(rect,b);
//             }else{
//                 EditorGUI.LabelField(rect,$"unknown primate type {metaValue.value.GetType()}");
//             }
//             if(EditorGUI.EndChangeCheck()){
//                 ValueEntry.SmartValue = metaValue;
//             }
//         }
//     }
// }
