using BBBirder.UnityVue;
using Sirenix.OdinInspector;
using UnityEngine;

partial class CubeData : MonoBehaviour
{
    void Start()
    {
        CSReactive.Reactive(this);

        this.WatchEffect(() =>
        {
            var halfArea = (Length * Width) + (Width * Height) + (Length * Height);
            Area = halfArea * 2;
        });

        this.WatchEffect(() =>
        {
            Volume = Length * Width * Height;
        });

        this.Compute(() => Length + Width + Height, v => Sum = v);
    }
}

partial class CubeData : IDataProxy
{
    public float Length { get; set; } = 1;
    public float Width { get; set; } = 1;
    public float Height { get; set; } = 1;

    public float Sum { get; set; }
    public float Area { get; set; }
    public float Volume { get; set; }
}
