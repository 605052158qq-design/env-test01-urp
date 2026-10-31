using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    public ENVIntroCutscene introCutscene;
    public ENVBoatArrival boatArrival;
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
    private bool worldPaused;
    private float timeScaleBeforeMenu = 1f;

    private void Awake()
    {
        if (introSequence != null)
            introSequence.onComplete.AddListener(BeginBoatArrival);
        if (introCutscene != null)
            introCutscene.onComplete.AddListener(BeginBoatArrival);
        if (boatArrival != null)
            boatArrival.onArrivalComplete.AddListener(EnableGameplayInput);
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
            introSequence.onComplete.RemoveListener(BeginBoatArrival);
        if (introCutscene != null)
            introCutscene.onComplete.RemoveListener(BeginBoatArrival);
        if (boatArrival != null)
            boatArrival.onArrivalComplete.RemoveListener(EnableGameplayInput);
        SetWorldVisible(true);
        ResumeWorld();
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
        SetWorldVisible(false);
        PauseWorld();
    }

    public void BeginGame()
    {
        if (introPending) return;
        if (menuRoot != null) menuRoot.SetActive(false);
        ResumeWorld();
        SetWorldVisible(true);
#if UNITY_EDITOR
        StartCoroutine(CaptureValidationFrame("v1-l10-after-start.png"));
#endif
        // The supplied 4K clip is the menu background, not an intro movie.
        // This short placeholder cinematic intentionally gives L06 a clear handoff.
        if (introCutscene != null)
        {
            introPending = true;
            introCutscene.Play();
            return;
        }
        BeginBoatArrival();
    }

    public void ContinueGame()
    {
        if (menuRoot != null) menuRoot.SetActive(false);
        ResumeWorld();
        SetWorldVisible(true);
        EnableGameplayInput();
    }

    private void BeginBoatArrival()
    {
        if (boatArrival != null && boatArrival.CanPlay)
        {
            introPending = true;
            boatArrival.PlayArrival();
            return;
        }

        // Keep the prototype playable if an artist moves/removes a temporary marker.
        // The configuration tool reports the missing reference in the Console.
        EnableGameplayInput();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void EnableGameplayInput()
    {
        introPending = false;
        ResumeWorld();
        SetWorldVisible(true);
        SetGameplayInput(true);
    }

    private void PauseWorld()
    {
        if (worldPaused) return;
        timeScaleBeforeMenu = Time.timeScale;
        Time.timeScale = 0f;
        worldPaused = true;
    }

    private void ResumeWorld()
    {
        if (!worldPaused) return;
        Time.timeScale = timeScaleBeforeMenu;
        worldPaused = false;
    }

    private void SetWorldVisible(bool visible)
    {
        // Do not cache per-renderer enabled state here.  A script/domain reload while
        // the menu is visible would cache "false" as the authored state and leave the
        // town permanently invisible after Start.  MapRoot is the artist-authored
        // world container; toggling it is reversible and also hides gems/pointers.
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "MapRoot" || root.name == "pengzhuang_map1")
                root.SetActive(visible);
        }
    }

    private void SetGameplayInput(bool enabled)
    {
        if (gameplayInput != null)
        {
            foreach (Behaviour behaviour in gameplayInput)
                if (behaviour != null) behaviour.enabled = enabled;
        }

        // Existing scenes may have been installed before the camera was added to
        // gameplayInput. Resolve it here as well so a menu always releases the mouse.
        ENVPrototypeOrbitCamera orbitCamera = FindObjectOfType<ENVPrototypeOrbitCamera>(true);
        if (orbitCamera != null) orbitCamera.enabled = enabled;

        // Region UI uses unscaled time so it remains legible during cinematics.
        // Keep all prototype region/gem validation logic dormant behind the menu;
        // otherwise its Start/trigger callbacks can expose “微光码头” before play begins.
        foreach (ENVRegionTrigger trigger in FindObjectsOfType<ENVRegionTrigger>(true))
            trigger.enabled = enabled;
        foreach (ENVVerticalSliceRunner runner in FindObjectsOfType<ENVVerticalSliceRunner>(true))
            runner.enabled = enabled;
        foreach (ENVObjectivePointer objectivePointer in FindObjectsOfType<ENVObjectivePointer>(true))
            objectivePointer.enabled = enabled;
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
