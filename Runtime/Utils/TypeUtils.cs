using System;
using System.Collections.Generic;
using System.Reflection;

namespace com.bbbirder{
    internal static class TypeUtils{
        const BindingFlags StaticBindingFlags = 0
            | BindingFlags.Static
            | BindingFlags.NonPublic
            | BindingFlags.Public
            ;
        static Dictionary<Type,Func<object,object>> s_casters = new();
        static MethodInfo s_method_resig;
        internal static T CastTo<T>(object obj){
            return (T)GetCastDelegate(obj.GetType(),typeof(T))?.Invoke(obj);
        }
        static Func<object,object> GetCastDelegate(this Type from, Type to){
            if(s_casters.TryGetValue(from,out var caster)){
                return caster;
            }
            var mi = GetCastMethod(from,to);
            if(mi is null){
                return null;
            }
            s_method_resig ??= typeof(TypeUtils).GetMethod(nameof(ResigFunc),StaticBindingFlags);
            var f = s_method_resig.MakeGenericMethod(from,to).Invoke(null,new[]{mi});
            return f as Func<object,object>;
        }
        static Func<object,object> ResigFunc<T1,T2>(MethodInfo mi){
            var func = mi.CreateDelegate(typeof(Func<T1,T2>)) as Func<T1,T2>;
            return o=>(object)func.Invoke((T1)o);
        }
        static MethodInfo GetCastMethod(this Type from, Type to)
        {
            var allMembers = from.GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (MethodInfo item in allMembers)
            {
                if ((item.Name == "op_Implicit" || item.Name == "op_Explicit") && item.GetParameters()[0].ParameterType.IsAssignableFrom(from) && to.IsAssignableFrom(item.ReturnType))
                {
                    return item;
                }
            }
            allMembers = to.GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (MethodInfo item in allMembers)
            {
                if ((item.Name == "op_Implicit" || item.Name == "op_Explicit") && item.GetParameters()[0].ParameterType.IsAssignableFrom(from) && to.IsAssignableFrom(item.ReturnType))
                {
                    return item;
                }
            }
            return null;
        }
    }
}