using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A01/A02/A03. One fullscreen URP pass, original URP/Lit map materials stay unchanged.
/// Add this Renderer Feature to your existing ENV_ForwardRenderer asset.
/// Requires URP 14 in Unity 2022.3 and a camera depth texture.
/// </summary>
public sealed class ENVColorRecoveryFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public sealed class Options
    {
        public Shader shader;
        [Range(0f, 1f)] public float grayscaleStrength = 1f;
        [Min(0.01f)] public float edgeSoftness = 1.5f;
        [Range(0f, 2f)] public float whiteRingIntensity = 0.8f;
        [Min(0.01f)] public float whiteRingWidth = 0.6f;
        public bool showInSceneView = false;
    }
    public Options settings = new Options();
    private ENVPass pass;
    private Material material;

    public override void Create()
    {
        if (settings.shader == null) settings.shader = Shader.Find("Hidden/ENV/ColorRecovery");
        if (settings.shader == null) { Debug.LogWarning("ENV ColorRecovery shader not found"); return; }
        if (material == null || material.shader != settings.shader)
        {
            if (material != null) CoreUtils.Destroy(material);
            material = CoreUtils.CreateEngineMaterial(settings.shader);
        }
        if (pass == null) pass = new ENVPass();
        pass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        pass.ConfigureInput(ScriptableRenderPassInput.Depth);
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass == null || material == null || !Application.isPlaying || ENVWorldProgress.Instance == null || ENVCinematicScreen.ActiveSequences > 0) return;
        CameraType type = renderingData.cameraData.cameraType;
        if (type == CameraType.Preview || type == CameraType.Reflection ||
            (type == CameraType.SceneView && !settings.showInSceneView)) return;
        pass.SetMaterial(material, settings);
        renderer.EnqueuePass(pass);
    }
    protected override void Dispose(bool disposing)
    {
        if (pass != null) pass.Release();
        if (material != null) CoreUtils.Destroy(material);
        material = null;
    }

    private sealed class ENVPass : ScriptableRenderPass
    {
        private Material material;
        private Options options;
        private RTHandle temp;
        private const int Max = 16;
        private static readonly Vector4[] circles = new Vector4[Max];
        private static readonly int CountID = Shader.PropertyToID("_ENVCount");
        private static readonly int CirclesID = Shader.PropertyToID("_ENVCircles");
        private static readonly int SoftnessID = Shader.PropertyToID("_ENVSoftness");
        private static readonly int GrayID = Shader.PropertyToID("_ENVGray");
        private static readonly int RingWidthID = Shader.PropertyToID("_ENVRingWidth");
        private static readonly int RingStrengthID = Shader.PropertyToID("_ENVRingStrength");
        public void SetMaterial(Material mat, Options opts) { material = mat; options = opts; }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(ref temp, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_ENVRecoveryTemp");
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || temp == null) return;
            var world = ENVWorldProgress.Instance;
            if (world == null) return;
            int count = 0;
            foreach (var g in world.Gems)
            {
                if (count >= Max) break;
                if (g == null || !g.Activated) continue;
                Vector3 p = g.RevealPosition;
                circles[count++] = new Vector4(p.x, p.z, g.RadiusNow, g.RadiusNow < g.revealMaxRadius - 0.01f ? 1f : 0f);
            }
            material.SetInt(CountID, count);
            material.SetVectorArray(CirclesID, circles);
            material.SetFloat(SoftnessID, options.edgeSoftness);
            material.SetFloat(GrayID, options.grayscaleStrength);
            material.SetFloat(RingWidthID, options.whiteRingWidth);
            material.SetFloat(RingStrengthID, options.whiteRingIntensity);
            var cmd = CommandBufferPool.Get("ENV Restore World Colors");
            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            Blitter.BlitCameraTexture(cmd, source, temp, material, 0);
            Blitter.BlitCameraTexture(cmd, temp, source);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        public void Release() { if (temp != null) temp.Release(); temp = null; }
    }
}
