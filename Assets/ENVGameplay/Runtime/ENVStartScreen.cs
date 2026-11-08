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

    [Header("Menu audio")]
    [Tooltip("Played once when the pointer enters a menu action.")]
    public AudioClip hoverAudio;
    [Tooltip("Played when Start, Continue, or Exit is clicked.")]
    public AudioClip clickAudio;
    [Range(0f, 1f)] public float menuAudioVolume = 0.65f;

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
    private AudioSource menuAudioSource;
    private int hoveredAction;

    private void Awake()
    {
        menuAudioSource = GetComponent<AudioSource>();
        if (menuAudioSource == null) menuAudioSource = gameObject.AddComponent<AudioSource>();
        menuAudioSource.playOnAwake = false;
        menuAudioSource.spatialBlend = 0f;
        menuAudioSource.loop = false;
        ENVAudioBank audioBank = FindObjectOfType<ENVAudioBank>(true);
        if (audioBank != null)
        {
            if (hoverAudio == null) hoverAudio = audioBank.uiHover;
            if (clickAudio == null) clickAudio = audioBank.uiClick;
        }

        if (introSequence != null)
            introSequence.onComplete.AddListener(BeginBoatArrival);
        if (introCutscene != null)
            introCutscene.onComplete.AddListener(BeginBoatArrival);
        if (boatArrival != null)
            boatArrival.onArrivalComplete.AddListener(EnableGameplayInput);
        ShowMenu();
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
        // Escape skips only an active opening video/caption. It does not skip the
        // boat crossing or restore player input early.
        if (introPending && Input.GetKeyDown(KeyCode.Escape))
        {
            if (introSequence != null && introSequence.IsPlaying)
            {
                introSequence.Skip();
                return;
            }
            if (introCutscene != null && introCutscene.IsPlaying)
            {
                introCutscene.Skip();
                return;
            }
        }
        if (menuRoot == null || !menuRoot.activeSelf) return;

        Vector2 pointer = new Vector2(
            Mathf.Clamp01(Input.mousePosition.x / Mathf.Max(1f, Screen.width)),
            1f - Mathf.Clamp01(Input.mousePosition.y / Mathf.Max(1f, Screen.height)));
        bool overStart = startRegion.Contains(pointer);
        bool overContinue = continueRegion.Contains(pointer);
        bool overExit = exitRegion.Contains(pointer);
        int currentHoveredAction = overStart ? 1 : overContinue ? 2 : overExit ? 3 : 0;
        if (currentHoveredAction != 0 && currentHoveredAction != hoveredAction)
            PlayMenuAudio(hoverAudio);
        hoveredAction = currentHoveredAction;
        SetHighlight(startHighlight, overStart);
        SetHighlight(continueHighlight, overContinue);
        SetHighlight(exitHighlight, overExit);

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) ||
            (Input.GetMouseButtonDown(0) && (overStart || overContinue)))
        {
            if (Input.GetMouseButtonDown(0)) PlayMenuAudio(clickAudio);
            BeginGame();
        }
        else if (Input.GetMouseButtonDown(0) && overExit)
        {
            PlayMenuAudio(clickAudio);
            QuitGame();
        }
    }

    public void ShowMenu()
    {
        introPending = false;
        hoveredAction = 0;
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

        // The supplied 4K clip is the menu background, not an intro movie.
        // Prefer the authored gameplay intro; retain the caption card as a fallback
        // while a project is being assembled without its video asset.
        if (introSequence != null && introSequence.suppliedVideo != null)
        {
            introPending = true;
            introSequence.Play();
            return;
        }
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
            // The video used CameraNearPlane, so it never needs a camera handoff.
            // Resume only the orbit camera now; player input remains frozen by the
            // boat passenger ride until arrival completes.
            ENVPrototypeOrbitCamera orbitCamera = FindObjectOfType<ENVPrototypeOrbitCamera>(true);
            if (orbitCamera != null) orbitCamera.enabled = true;
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

    private void PlayMenuAudio(AudioClip clip)
    {
        if (menuAudioSource != null && clip != null)
            menuAudioSource.PlayOneShot(clip, menuAudioVolume);
    }


}
