using UnityEngine;

// ONLY attached to objects in the isolated preview scene. Do not add to gameplay objects.
[DisallowMultipleComponent]
public sealed class ENVShaderKitPreviewOnly : MonoBehaviour
{
    public enum Mode { Dissolve, Ring }
    public Mode mode = Mode.Dissolve;
    public Renderer target;
    private MaterialPropertyBlock props;
    private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");

    void LateUpdate()
    {
        if (target == null) target = GetComponent<Renderer>();
        if (target == null) return;
        if (props == null) props = new MaterialPropertyBlock();
        target.GetPropertyBlock(props);
        if (mode == Mode.Dissolve) props.SetFloat(DissolveId, Mathf.PingPong(Time.time * .3f, 1f));
        else props.SetFloat(RadiusId, 0.04f + Mathf.PingPong(Time.time * .22f, .5f));
        target.SetPropertyBlock(props);
    }
}
