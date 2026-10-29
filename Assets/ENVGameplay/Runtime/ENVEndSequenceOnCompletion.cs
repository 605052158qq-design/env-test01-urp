using UnityEngine;

/// <summary>L13: routes last gemstone's completion to the SAME video object; no second fake scene.</summary>
[DisallowMultipleComponent]
public sealed class ENVEndSequenceOnCompletion : MonoBehaviour
{
    public ENVWorldProgress world;
    public ENVVideoSequence endingVideo;
    private void Start()
    {
        if (world == null) world = ENVWorldProgress.Instance;
        if (world != null) world.onAllGemsActivated.AddListener(OnDone);
    }
    private void OnDestroy() { if (world != null) world.onAllGemsActivated.RemoveListener(OnDone); }
    private void OnDone() { if (endingVideo != null) endingVideo.Play(); }
}
