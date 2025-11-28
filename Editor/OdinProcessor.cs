#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace BBBirder.UnityVue.Editor
{
    public class RefDataOdinProcessor<T> : OdinAttributeProcessor<RefData<T>>
    {
        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
        {
            if (member.Name == nameof(RefData<int>.Value))
                attributes.Add(new ShowInInspectorAttribute());
        }
        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
        {
            attributes.Add(new HideReferenceObjectPickerAttribute());
        }
    }

    public class ComputedOdinProcessor<T> : OdinAttributeProcessor<Computed<T>>
    {
        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
        {
            attributes.Add(new HideReferenceObjectPickerAttribute());
        }
        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
        {
            if (member.Name == nameof(Computed<T>.Value))
            {
                attributes.Add(new ShowInInspectorAttribute());
                attributes.Add(new DisplayAsStringAttribute());
            }
        }
    }

    public class DataProxyOdinProcessor : OdinAttributeProcessor<IDataProxy>
    {
        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
        {
            if (member is PropertyInfo property && IDataProxy.IsPropertyValid(property))
                attributes.Add(new ShowInInspectorAttribute());
        }
    }

    public class NotifyAttributeOdinProcessor : OdinAttributeProcessor<ReactiveBehaviour>
    {
        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
        {
            var notifyAttribute = attributes.OfType<NotifyAttribute>().FirstOrDefault();
            if (notifyAttribute != null)
            {
                attributes.Add(new ShowInInspectorAttribute());
                attributes.Add(new OnValueChangedAttribute($"@(this as IWatchable).Payload.onAfterSet?.Invoke(this,\"{notifyAttribute.TargetProperty}\")"));
            }
        }
    }
}
#endif
