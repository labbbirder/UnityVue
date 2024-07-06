using BBBirder.UnityVue;
using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteAlways]
partial class CubeData : MonoBehaviour
{

    void Awake()
    {
        CSReactive.Reactive(this);
    }
}

partial class CubeData : IDataProxy
{
    [ShowInInspector]
    public float Length { get; set; }
    [ShowInInspector]
    public float Width { get; set; }
    [ShowInInspector]
    public float Height { get; set; }

    [ShowInInspector]
    public float Area { get; set; }

    [ShowInInspector]
    public float Area { get; set; }
}