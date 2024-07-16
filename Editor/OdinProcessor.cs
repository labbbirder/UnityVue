#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace BBBirder.UnityVue.Editor
{
    public class OdinProcessor : OdinAttributeProcessor<IDataProxy>
    {
        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
        {
            if (member is PropertyInfo property && IDataProxy.IsPropertyValid(property))
                attributes.Add(new ShowInInspectorAttribute());
        }
    }
}
#endif
