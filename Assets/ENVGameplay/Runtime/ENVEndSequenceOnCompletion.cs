using UnityEngine;

/// <summary>L13: routes last gemstone's completion to the SAME video object; no second fake scene.</summary>
[DisallowMultipleComponent]
public sealed class ENVEndSequenceOnCompletion : MonoBehaviour
{
    public ENVWorldProgress world;
    public ENVVideoSequence endingVideo;
    [Tooltip("Used until the supplied ending video is available.")]
    public ENVRegionTitleUI titleUi;
    public string completionMessage = "七色宝石已归位 · 微光小镇恢复色彩";
    private void Start()
    {
        if (world == null) world = ENVWorldProgress.Instance;
        if (world != null) world.onAllGemsActivated.AddListener(OnDone);
    }
    private void OnDestroy() { if (world != null) world.onAllGemsActivated.RemoveListener(OnDone); }
    private void OnDone()
    {
        if (endingVideo != null)
        {
            endingVideo.Play();
            return;
        }
        if (titleUi == null) titleUi = ENVRegionTitleUI.Instance;
        if (titleUi != null) titleUi.Show(completionMessage, 4f);
    }

    /// <summary>Invoked by the V1 debug GUI when validating the end branch.</summary>
    public void DebugComplete() { OnDone(); }
}
