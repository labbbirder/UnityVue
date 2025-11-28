using System;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using static BBBirder.UnityVue.CastUtils;

namespace BBBirder.UnityVue
{
    public static class UIToolKitExtensions
    {
#warning TODO: 非DEBUG模式下可以考虑更高效的实现
        public static WatchScope Bind<T>(this INotifyValueChanged<T> control, Expression<Func<object>> getter)
        {
            var bodyExp = getter.Body is UnaryExpression castOp ? castOp.Operand : getter.Body;
            var memberExp = bodyExp as MemberExpression;
            Assert.IsNotNull(memberExp, $"{bodyExp.NodeType} must be a member getter");
            var property = memberExp.Member as PropertyInfo;
            Assert.IsNotNull(memberExp, "member must be a property");
            var targetGetter = Expression.Lambda(memberExp.Expression).Compile() as Func<object>;
            var target = targetGetter();

            Func<T, object> memberSetCaster = property.PropertyType.IsAssignableFrom(typeof(T)) ?
                null : v => DynamicCast(v, property.PropertyType);
            Func<object, T> ctrlSetCaster = typeof(T).IsAssignableFrom(property.PropertyType) ?
                null : m => (T)DynamicCast(m, typeof(T));
            control.RegisterValueChangedCallback(e =>
            {
                property.SetValue(target, memberSetCaster != null ? memberSetCaster(e.newValue) : e.newValue);
            });
            var scope = CSReactive.WatchEffect(() =>
            {
                var memberValue = property.GetValue(target);
                control.SetValueWithoutNotify(ctrlSetCaster != null ? ctrlSetCaster(memberValue) : (T)memberValue);
            });
            return scope;
        }
    }
}
