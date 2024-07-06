// using System;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;
// using UnityEngine;

// namespace com.bbbirder{
//     public static partial class JsonExtensions{
//         /// <summary>
//         /// Convert a json type to a watched meta tpye
//         /// </summary>
//         /// <typeparam name="T"></typeparam>
//         /// <param name="token"></param>
//         /// <returns></returns> <summary>
//         /// 
//         /// </summary>
//         /// <param name="token"></param>
//         /// <typeparam name="T"></typeparam>
//         /// <returns></returns>
//         public static MetaToken ConvertToMeta<T>(this T token) where T:JToken{

//             if(token is null) return null;
//             if(token is JArray arr){
//                 var res = new MetaArray();
//                 foreach(var ele in arr){
//                     var tk = ConvertToMeta(ele);
//                     res.Add(tk);
//                 }
//                 return res;
//             }
//             if(token is JObject obj){
//                 var res = new MetaObject();
//                 foreach(var (k,v) in obj){
//                     var tk = ConvertToMeta(v);
//                     res[k] = tk;
//                 }
//                 return res;
//             }
//             if(token is JValue val){
//                 return MetaValue.Create(val.Value);
//             }
//             throw new($"Unknown type json token {token.GetType()}");
//         }

//         public static MetaToken Reactive(this CSReactive.InternalMaker maker, JToken token){
//             return maker.SetProxy(token.ConvertToMeta());
//         }
//         public static MetaToken ReactiveJson(this CSReactive.InternalMaker maker, string json){
//             return maker.SetProxy(JsonConvert.DeserializeObject<MetaToken>(json));
//         }
//     }

//     [JsonConverter(typeof(MetaConverter))]
//     partial class MetaToken { }
//     public class MetaConverter : JsonConverter<MetaToken>
//     {
//         // public override bool CanConvert(Type objectType)
//         // {
//         //     return objectType.IsSubclassOf(typeof(MetaToken));
//         // }
//         public JsonToken GetJsonToken(MetaValue metaValue){
//             var value = metaValue?.value;
//             if(value is null) return JsonToken.Null;
//             if(value is int ) return JsonToken.Integer;
//             if(value is string ) return JsonToken.String;
//             if(value is float ) return JsonToken.Float;
//             if(value is bool ) return JsonToken.Boolean;
//             if(value is long ) return JsonToken.Integer;
//             if(value is double ) return JsonToken.Float;
//             throw new ($"Unknown metaValue type {value.GetType()}");
//         }
//         public override void WriteJson(JsonWriter writer, MetaToken value, JsonSerializer serializer)
//         {
//             if(value == null) {
//                 writer.WriteNull();
//                 return;
//             }
//             if(value is MetaValue val){
//                 writer.WriteToken(GetJsonToken(val),val.value);
//                 return;
//             }
//             if(value is MetaArray arr){
//                 writer.WriteStartArray();
//                 foreach(var tk in arr){
//                     WriteJson(writer,tk,serializer);
//                 }
//                 writer.WriteEndArray();
//                 return;
//             }
//             if(value is MetaObject obj){
//                 writer.WriteStartObject();
//                 foreach(var (name,tk) in obj){
//                     writer.WritePropertyName(name);
//                     WriteJson(writer,tk,serializer);
//                 }
//                 writer.WriteEndObject();
//                 return;
//             }
//             throw new ($"Unknown metaToken type {value.GetType()}");
//         }

//         public override MetaToken ReadJson(JsonReader reader, Type objectType, MetaToken existingValue,bool hasExistingValue, JsonSerializer serializer)
//         {
//             var jToken = serializer.Deserialize<JToken>(reader);
//             return jToken.ConvertToMeta();
//         }

//         // public override void WriteJson(JsonWriter writer, MetaToken value, JsonSerializer serializer)
//         // {
//         //     throw new NotImplementedException();
//         // }

//         // public override MetaToken ReadJson(JsonReader reader, Type objectType, MetaToken existingValue, bool hasExistingValue, JsonSerializer serializer)
//         // {
//         //     throw new NotImplementedException();
//         // }
//     }
// }
