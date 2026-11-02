using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP 14 water-only SSR producer. Renders actual transparent water meshes into a
/// screen-aligned reflection RT BEFORE the normal transparent pass.
/// Traces reflected rays against the opaque camera depth; RGB=opaque scene color, A=hit mask.
/// This is screen-space reflection, NOT planar-camera reflection, and has screen-space limitations.
/// </summary>
public sealed class ENVWaterSSRFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public sealed class Options
    {
        [Range(0.25f, 1f)] public float renderScale = 0.5f;
        [Range(8, 64)] public int raySteps = 24;
        [Min(1f)] public float maxRayDistance = 35f;
        [Range(0.05f, 3f)] public float rayThickness = 0.65f;
        [Range(0f, 0.2f)] public float waterOffset = 0.035f;
        [Range(0f, 0.3f)] public float screenEdgeFade = 0.1f;
        public bool renderInSceneView = true;
        [Tooltip("0=SSR, 1=magenta water footprint, 2=cyan if depth-visible, 3=green SSR hit / red miss")]
        [Range(0, 3)] public int debugMode = 0;
        [Tooltip("Every 180 frames, report registered water sources and submitted draw calls.")]
        public bool logDiagnostics = false;
    }

    public Options settings = new Options();
    public Shader traceShader;
    private Material traceMaterial;
    private WaterReflectionPass pass;

    public override void Create()
    {
        if (traceShader == null) traceShader = Shader.Find("Hidden/ENV/WaterSSRTrace");
        if (traceMaterial != null) CoreUtils.Destroy(traceMaterial);
        traceMaterial = traceShader != null ? CoreUtils.CreateEngineMaterial(traceShader) : null;
        pass = new WaterReflectionPass(settings, traceMaterial);
        pass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass == null || traceMaterial == null) return;
        CameraData c = renderingData.cameraData;
        if (c.cameraType != CameraType.Game && !(settings.renderInSceneView && c.isSceneViewCamera)) return;
        if (c.renderType != CameraRenderType.Base) return;
        // Request a copy of the opaque scene color and depth. These are sampled by the SSR shader.
        pass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
        pass = null;
        CoreUtils.Destroy(traceMaterial);
        traceMaterial = null;
    }

    private sealed class WaterReflectionPass : ScriptableRenderPass
    {
        private readonly Options options;
        private readonly Material material;
        private RTHandle target;
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        private static readonly int ReflectionId = Shader.PropertyToID("_ReflectionRT");
        private static readonly int ReflectionEnabledId = Shader.PropertyToID("_UseReflectionRT");
        private static readonly int ReflectionAlphaId = Shader.PropertyToID("_ReflectionUseAlpha");
        private static readonly int GlobalSSRId = Shader.PropertyToID("_ENV_WaterSSRTexture");
        private static readonly int StepsId = Shader.PropertyToID("_ENVSSRSteps");
        private static readonly int RangeId = Shader.PropertyToID("_ENVSSRMaxDistance");
        private static readonly int ThicknessId = Shader.PropertyToID("_ENVSSRThickness");
        private static readonly int OffsetId = Shader.PropertyToID("_ENVSSROffset");
        private static readonly int EdgeFadeId = Shader.PropertyToID("_ENVSSREdgeFade");
        private static readonly int DebugModeId = Shader.PropertyToID("_ENVSSRDebugMode");

        internal WaterReflectionPass(Options o, Material m) { options = o; material = m; }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.width = Mathf.Max(1, Mathf.RoundToInt(desc.width * options.renderScale));
            desc.height = Mathf.Max(1, Mathf.RoundToInt(desc.height * options.renderScale));
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;
            desc.useMipMap = false;
            desc.autoGenerateMips = false;
            // For mobile use a light-weight RGBA8 RT; RGB is reflection, A is intersection validity.
            desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            RenderingUtils.ReAllocateIfNeeded(ref target, desc, FilterMode.Bilinear, TextureWrapMode.Clamp,
                name: "_ENV_WaterSSRTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (target == null || material == null) return;

            material.SetFloat(StepsId, options.raySteps);
            material.SetFloat(RangeId, options.maxRayDistance);
            material.SetFloat(ThicknessId, options.rayThickness);
            material.SetFloat(OffsetId, options.waterOffset);
            material.SetFloat(EdgeFadeId, options.screenEdgeFade);
            material.SetFloat(DebugModeId, options.debugMode);

            CommandBuffer cmd = CommandBufferPool.Get("ENV Water SSR / Trace Water Meshes");
            CoreUtils.SetRenderTarget(cmd, target, ClearFlag.Color, Color.clear);

            int eligibleWater = 0;
            int submittedSubmeshes = 0;
            foreach (ENVWaterSSRSource source in ENVWaterSSRSource.Active)
            {
                if (source == null || !source.isActiveAndEnabled) continue;
                Renderer water = source.targetRenderer;
                if (water == null || !water.enabled || !water.gameObject.activeInHierarchy) continue;
                eligibleWater++;
                // Trace once per visible water mesh/submesh, independent of its transparent material.
                var materials = water.sharedMaterials;
                for (int sub = 0; sub < materials.Length; sub++)
                {
                    if (materials[sub] == null) continue;
                    cmd.DrawRenderer(water, material, sub, 0);
                    submittedSubmeshes++;
                }

                water.GetPropertyBlock(propertyBlock);
                propertyBlock.SetTexture(ReflectionId, target.rt);
                propertyBlock.SetFloat(ReflectionEnabledId, 1f);
                propertyBlock.SetFloat(ReflectionAlphaId, 1f);
                water.SetPropertyBlock(propertyBlock);
            }
            cmd.SetGlobalTexture(GlobalSSRId, target.nameID);
            if (options.logDiagnostics && Time.frameCount % 180 == 0)
            {
                Debug.Log($"[ENV Water SSR] camera={renderingData.cameraData.camera.name}, " +
                    $"activeSources={ENVWaterSSRSource.Active.Count}, validWater={eligibleWater}, " +
                    $"submittedSubmeshes={submittedSubmeshes}, debugMode={options.debugMode}. " +
                    "If 0, check source components and actual water mesh renderers.");
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        internal void Dispose() { target?.Release(); target = null; }
    }
}
