using System.Collections;
using System.IO;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Temporary V1 vertical-slice harness.  It uses the real first named gemstone,
/// the existing player and the normal ENVGemInteractor path.  In the editor G
/// moves the player beside that gem so the actual E interaction can be verified
/// without changing the authored spawn location.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVVerticalSliceRunner : MonoBehaviour
{
    public Transform player;
    public ENVGem sampleGem;
    public ENVRegionTitleUI titleUi;
    public string regionTitle = "微光码头";
    [Tooltip("Editor-only evidence automation. Keep off for normal manual play.")]
    public bool captureEditorEvidence;
    private bool gemEvidenceCaptured;

    private void Start()
    {
        if (titleUi != null) titleUi.Show(regionTitle, 3.5f);
        if (sampleGem != null) sampleGem.onActivated.AddListener(OnSampleGemActivated);
#if UNITY_EDITOR
        if (captureEditorEvidence) StartCoroutine(RunEditorEvidenceSequence());
#endif
    }

    private void OnDestroy()
    {
        if (sampleGem != null) sampleGem.onActivated.RemoveListener(OnSampleGemActivated);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (GPressed()) PlacePlayerBesideGem();
#endif
    }

    private void OnSampleGemActivated()
    {
        if (titleUi != null) titleUi.Show("蓝宝石已触碰 · 下一个目标已更新", 3.5f);
#if UNITY_EDITOR
        if (!gemEvidenceCaptured)
        {
            gemEvidenceCaptured = true;
            StartCoroutine(CaptureAfterFrame("v1-slice-gem-activated.png"));
        }
#endif
    }

#if UNITY_EDITOR
    private IEnumerator RunEditorEvidenceSequence()
    {
        ENVStartScreen startScreen = FindObjectOfType<ENVStartScreen>();
        if (startScreen != null) startScreen.ContinueGame();
        if (titleUi != null) titleUi.Show(regionTitle, 3.5f);
        yield return CaptureAfterFrame("v1-slice-region-trigger.png");
        yield return new WaitForSeconds(0.8f);
        PlacePlayerBesideGem();
        yield return null;
        ENVGemInteractor interactor = player != null ? player.GetComponent<ENVGemInteractor>() : null;
        if (interactor != null && interactor.FindNearest() == sampleGem)
            sampleGem.TryActivate();
        else
            Debug.LogError("[ENV Slice] Editor evidence could not reach the configured real gem.", this);
    }

    private void PlacePlayerBesideGem()
    {
        if (player == null || sampleGem == null || sampleGem.Activated) return;
        CharacterController controller = player.GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;
        if (controller != null) controller.enabled = false;
        Vector3 offset = sampleGem.transform.forward.sqrMagnitude > 0.01f
            ? -sampleGem.transform.forward.normalized * 1.25f
            : Vector3.back * 1.25f;
        player.position = sampleGem.transform.position + offset;
        player.LookAt(new Vector3(sampleGem.transform.position.x, player.position.y, sampleGem.transform.position.z));
        if (controller != null) controller.enabled = wasEnabled;
    }

    private void OnGUI()
    {
        if (sampleGem == null || sampleGem.Activated) return;
        GUI.Label(new Rect(24f, Screen.height - 50f, 460f, 28f), "V1 验证：G 前往蓝宝石，按 E 触碰");
    }

    private static bool GPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.G);
#endif
    }

    private static IEnumerator CaptureAfterFrame(string fileName)
    {
        yield return new WaitForEndOfFrame();
        string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ValidationScreenshots");
        Directory.CreateDirectory(folder);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, fileName));
    }
#endif
}
