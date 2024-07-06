using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;

static partial class SerializedPropertyExtension
{
    public static void RemoveFromArray(this SerializedProperty property)
    {
        if (property.TryGetArrayParent(out var res))
        {
            var (parent, index) = res;
            parent.DeleteArrayElementAtIndex(index);
        }
        else
        {
            throw new System.Exception($"cannot find array-like parent for property {property}");
        }
    }

    public static int GetElementIndex(this SerializedProperty property)
    {
        if (property.TryGetArrayParent(out var res))
        {
            return res.index;
        }
        return ~0;
    }

    public static bool TryGetParent(this SerializedProperty property, out SerializedProperty parentProperty)
    {
        if (TryGetArrayParent(property, out var res))
        {
            ; (parentProperty, _) = res;
            return true;
        }
        else if (TryGetObjectParent(property, out parentProperty))
        {
            return true;
        }

        return false;
    }

    static bool TryGetArrayParent(this SerializedProperty property, out (SerializedProperty parent, int index) res)
    {
        res = default;
        var match = Regex.Match(property.propertyPath, @"^(.*)\.Array\.data\[(\d+)\]$");
        if (match.Success)
        {
            var parentPath = match.Groups[1].ToString();
            var index = int.Parse(match.Groups[2].ToString());
            var propParent = property.serializedObject.FindProperty(parentPath);
            res = (propParent, index);
        }
        return match.Success;
    }

    static bool TryGetObjectParent(this SerializedProperty property, out SerializedProperty res)
    {
        res = default;
        var match = Regex.Match(property.propertyPath, @"^(.*)\.[^\.]+$");
        if (match.Success)
        {
            var parentPath = match.Groups[1].ToString();
            var propParent = property.serializedObject.FindProperty(parentPath);
            res = propParent;
        }
        return match.Success;
    }
}