using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// L10 minimum playable opening: shows the supplied menu art, waits for an
/// explicit Start/Continue input, then plays the supplied intro clip when one
/// is assigned before enabling player input.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVStartScreen : MonoBehaviour
{
    [Header("Scene references")]
    public GameObject menuRoot;
    public ENVVideoSequence introSequence;
    public Behaviour[] gameplayInput;

    [Header("Menu art")]
    public Image startHighlight;
    public Image continueHighlight;
    public Image exitHighlight;

    // Normalized screen rectangles. They match the supplied 1920x1080 menu
    // composition and deliberately keep the clickable regions independent of
    // the full-screen source PNGs.
    [Header("Click regions, normalized from top-left")]
    public Rect startRegion = new Rect(0.08f, 0.51f, 0.18f, 0.08f);
    public Rect continueRegion = new Rect(0.08f, 0.64f, 0.18f, 0.08f);
    public Rect exitRegion = new Rect(0.08f, 0.76f, 0.18f, 0.08f);

    private bool introPending;

    private void Awake()
    {
        if (introSequence != null)
            introSequence.onComplete.AddListener(EnableGameplayInput);
        ShowMenu();
    }

    private IEnumerator Start()
    {
#if UNITY_EDITOR
        // This is editor-only evidence for the iteration checklist. It never runs
        // in a player build and records the actual Game view after the Canvas is drawn.
        yield return CaptureValidationFrame("v1-l10-start-screen.png");
#else
        yield break;
#endif
    }

    private void OnDestroy()
    {
        if (introSequence != null)
            introSequence.onComplete.RemoveListener(EnableGameplayInput);
    }

    private void Update()
    {
        if (menuRoot == null || !menuRoot.activeSelf) return;

        Vector2 pointer = new Vector2(
            Mathf.Clamp01(Input.mousePosition.x / Mathf.Max(1f, Screen.width)),
            1f - Mathf.Clamp01(Input.mousePosition.y / Mathf.Max(1f, Screen.height)));
        bool overStart = startRegion.Contains(pointer);
        bool overContinue = continueRegion.Contains(pointer);
        bool overExit = exitRegion.Contains(pointer);
        SetHighlight(startHighlight, overStart);
        SetHighlight(continueHighlight, overContinue);
        SetHighlight(exitHighlight, overExit);

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) ||
            (Input.GetMouseButtonDown(0) && (overStart || overContinue)))
        {
            BeginGame();
        }
        else if (Input.GetMouseButtonDown(0) && overExit)
        {
            QuitGame();
        }
    }

    public void ShowMenu()
    {
        introPending = false;
        if (menuRoot != null) menuRoot.SetActive(true);
        SetGameplayInput(false);
    }

    public void BeginGame()
    {
        if (introPending) return;
        if (menuRoot != null) menuRoot.SetActive(false);
#if UNITY_EDITOR
        StartCoroutine(CaptureValidationFrame("v1-l10-after-start.png"));
#endif
        if (introSequence != null && introSequence.suppliedVideo != null)
        {
            introPending = true;
            introSequence.Play();
            return;
        }
        EnableGameplayInput();
    }

    public void ContinueGame()
    {
        if (menuRoot != null) menuRoot.SetActive(false);
        EnableGameplayInput();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void EnableGameplayInput()
    {
        introPending = false;
        SetGameplayInput(true);
    }

    private void SetGameplayInput(bool enabled)
    {
        if (gameplayInput == null) return;
        foreach (Behaviour behaviour in gameplayInput)
            if (behaviour != null) behaviour.enabled = enabled;
    }

    private static void SetHighlight(Image target, bool enabled)
    {
        if (target != null && target.gameObject.activeSelf != enabled)
            target.gameObject.SetActive(enabled);
    }

#if UNITY_EDITOR
    private static IEnumerator CaptureValidationFrame(string fileName)
    {
        yield return new WaitForEndOfFrame();
        string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ValidationScreenshots");
        Directory.CreateDirectory(folder);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, fileName));
    }
#endif
}
