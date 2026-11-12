using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Runtime uGUI presentation for the supplied 浮光小镇 NPC encounter.</summary>
public sealed class ENVNpcDialogueUI : MonoBehaviour
{
    private enum Speaker { Narrator, Traveler, Fairy }

    private struct Line
    {
        public Speaker speaker;
        public string text;
        public bool blackScreen;
        public Line(Speaker speaker, string text, bool blackScreen = false)
        {
            this.speaker = speaker;
            this.text = text;
            this.blackScreen = blackScreen;
        }
    }

    private static ENVNpcDialogueUI active;
    private ENVFairyIntroTrigger owner;
    private Line[] lines;
    private int index;
    private bool entering;
    private Image background;
    private Image shade;
    private RawImage frame;
    private RawImage traveler;
    private RawImage fairy;
    private Text speakerText;
    private Text bodyText;
    private Text continueText;
    private Text blackScreenText;
    private Image fadeOverlay;

    public static bool IsShowingFor(ENVFairyIntroTrigger trigger)
    {
        return active != null && active.owner == trigger;
    }

    public static void Show(ENVFairyIntroTrigger trigger)
    {
        if (active != null) Destroy(active.gameObject);
        GameObject root = new GameObject("ENV NPC Dialogue", typeof(RectTransform));
        active = root.AddComponent<ENVNpcDialogueUI>();
        active.owner = trigger;
        try
        {
            active.Build(trigger);
            active.StartCoroutine(active.EnterDialogue());
        }
        catch (System.Exception exception)
        {
            // Never leave player input frozen if a future UI asset/configuration fails.
            Debug.LogException(exception, root);
            active = null;
            Destroy(root);
            if (trigger != null) trigger.FinishDialogue();
        }
    }

    public static void CloseWithoutCompleting()
    {
        if (active != null) Destroy(active.gameObject);
    }

    private void Build(ENVFairyIntroTrigger trigger)
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();

        background = CreateImage("Background", transform, trigger.dialogueBackground, Color.white);
        Stretch(background.rectTransform, Vector2.zero, Vector2.one);
        background.preserveAspect = false;

        shade = CreateImage("Narration Shade", transform, null, new Color(0f, 0f, 0f, 0.18f));
        Stretch(shade.rectTransform, Vector2.zero, Vector2.one);

        // Traveler is a root-level sibling placed before Dialogue Frame, so the
        // decorative frame correctly renders in front of the character.
        traveler = CreatePortrait("Traveler", transform, trigger.travelerPortrait,
            new Rect(114f / 1672f, 78f / 941f, 427f / 1672f, 584f / 941f));
        SetAnchors(traveler.rectTransform, new Vector2(0.07f, 0.18f), new Vector2(0.40f, 0.62f));
        ApplyInspectorOffsets(traveler.rectTransform, -142f, 56f, 142f, -56f);
        traveler.rectTransform.localScale = new Vector3(0.59f, 1f, 1f);

        // Keep the supplied visual hierarchy: Fairy is the direct child of
        // Dialogue Frame, so it layers above the frame artwork.
        frame = CreateCroppedFrame("Dialogue Frame", transform, trigger.dialogueFrame);
        SetAnchors(frame.rectTransform, new Vector2(0f, 0.035f), new Vector2(0.92f, 0.39f));

        fairy = CreatePortrait("Fairy", frame.transform, trigger.fairyPortrait,
            new Rect(1238f / 1672f, 224f / 941f, 246f / 1672f, 292f / 941f));
        SetAnchors(fairy.rectTransform, new Vector2(0.78f, 0.58f), new Vector2(1.03f, 1.39f));
        ApplyInspectorOffsets(fairy.rectTransform, -99.79999f, 3.29988f, 99.79999f, -3.29988f);
        fairy.rectTransform.localScale = new Vector3(0.66f, 1f, 1f);

        speakerText = CreateText("Speaker", transform, 32, FontStyle.Bold, new Color(0.96f, 0.95f, 0.98f));
        SetAnchors(speakerText.rectTransform, new Vector2(0.265f, 0.26f), new Vector2(0.46f, 0.31f));
        ApplyInspectorOffsets(speakerText.rectTransform, 0f, -7f, 0f, 7f);
        speakerText.alignment = TextAnchor.MiddleLeft;

        bodyText = CreateText("Dialogue", transform, 32, FontStyle.Normal, new Color(0.18f, 0.14f, 0.16f));
        SetAnchors(bodyText.rectTransform, new Vector2(0.225f, 0.125f), new Vector2(0.77f, 0.275f));
        ApplyInspectorOffsets(bodyText.rectTransform, 0f, 33f, 0f, -33f);
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        bodyText.lineSpacing = 1.15f;

        continueText = CreateText("Continue Hint", transform, 20, FontStyle.Normal, new Color(0.34f, 0.28f, 0.39f));
        SetAnchors(continueText.rectTransform, new Vector2(0.62f, 0.055f), new Vector2(0.78f, 0.085f));
        continueText.alignment = TextAnchor.MiddleRight;
        continueText.text = "点击 / 空格继续";

        blackScreenText = CreateText("Black Screen Text", transform, 42, FontStyle.Normal, Color.white);
        SetAnchors(blackScreenText.rectTransform, new Vector2(0.18f, 0.38f), new Vector2(0.82f, 0.62f));
        blackScreenText.alignment = TextAnchor.MiddleCenter;
        blackScreenText.horizontalOverflow = HorizontalWrapMode.Wrap;
        blackScreenText.verticalOverflow = VerticalWrapMode.Overflow;
        blackScreenText.lineSpacing = 1.25f;

        lines = CreateLines();
        SetContentVisible(false);

        fadeOverlay = CreateImage("Dialogue Fade", transform, null, Color.clear);
        Stretch(fadeOverlay.rectTransform, Vector2.zero, Vector2.one);
    }

    private void Update()
    {
        if (entering || lines == null || lines.Length == 0) return;
        if (MousePressed() || AdvancePressed()) Advance();
    }

    private IEnumerator EnterDialogue()
    {
        entering = true;
        yield return FadeTo(1f, 0.35f);
        SetContentVisible(true);
        ShowLine();
        yield return new WaitForSecondsRealtime(0.08f);
        yield return FadeTo(0f, 0.45f);
        entering = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float seconds)
    {
        float startAlpha = fadeOverlay != null ? fadeOverlay.color.a : 0f;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            if (fadeOverlay != null)
                fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(startAlpha, targetAlpha, elapsed / seconds));
            yield return null;
        }
        if (fadeOverlay != null) fadeOverlay.color = new Color(0f, 0f, 0f, targetAlpha);
    }

    private void Advance()
    {
        index++;
        if (index >= lines.Length)
        {
            ENVFairyIntroTrigger finishedOwner = owner;
            active = null;
            Destroy(gameObject);
            if (finishedOwner != null) finishedOwner.FinishDialogue();
            return;
        }
        ShowLine();
    }

    private void ShowLine()
    {
        Line line = lines[index];
        bool narration = line.speaker == Speaker.Narrator;
        bool blackScreen = line.blackScreen;
        background.enabled = !blackScreen;
        shade.enabled = true;
        shade.color = new Color(0f, 0f, 0f, blackScreen ? 1f : narration ? 0.56f : 0.18f);
        // Match the supplied layout: only the current speaker is on screen.
        frame.enabled = !blackScreen;
        traveler.enabled = !blackScreen && line.speaker == Speaker.Traveler && traveler.texture != null;
        fairy.enabled = !blackScreen && line.speaker == Speaker.Fairy && fairy.texture != null;
        blackScreenText.enabled = blackScreen;
        blackScreenText.text = blackScreen ? line.text : string.Empty;
        speakerText.enabled = !blackScreen;
        bodyText.enabled = !blackScreen;
        continueText.enabled = !blackScreen;
        speakerText.text = line.speaker == Speaker.Traveler ? "旅行家" :
            line.speaker == Speaker.Fairy ? "浮光精灵" : "旁白";
        bodyText.text = line.text;
        continueText.text = index == lines.Length - 1 ? "点击结束对话" : "点击 / 空格继续";
    }

    private void SetContentVisible(bool visible)
    {
        background.enabled = visible;
        shade.enabled = visible;
        frame.enabled = visible;
        traveler.enabled = false;
        fairy.enabled = false;
        speakerText.enabled = visible;
        bodyText.enabled = visible;
        continueText.enabled = visible;
        blackScreenText.enabled = false;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static RawImage CreateCroppedFrame(string name, Transform parent, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        RawImage image = go.GetComponent<RawImage>();
        image.texture = sprite != null ? sprite.texture : null;
        // RawImage UV origin is bottom-left. The artwork's usable frame is in
        // the bottom 43%, while the top is intentionally transparent padding.
        image.uvRect = new Rect(0f, 0f, 1f, 0.43f);
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static RawImage CreatePortrait(string name, Transform parent, Sprite sprite, Rect visibleUv)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        RawImage image = go.GetComponent<RawImage>();
        image.texture = sprite != null ? sprite.texture : null;
        image.uvRect = visibleUv;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(string name, Transform parent, int size, FontStyle style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Unity displays stretch offsets as Left / Top / Right / Bottom.  This
    // mapping deliberately preserves the exact values supplied in Inspector.
    private static void ApplyInspectorOffsets(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static bool MousePressed() { return Input.GetMouseButtonDown(0); }
    private static bool AdvancePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
    }

    private static Line[] CreateLines()
    {
        // One Word paragraph is exactly one click. Do not merge paragraphs and
        // do not add character-count pagination here.
        return new[]
        {
            new Line(Speaker.Narrator, "森林深处，树叶忽然沙沙作响。", true),
            new Line(Speaker.Narrator, "旅行家停下脚步，警惕地望向声音传来的方向。一点微弱的蓝光从树后闪过。", true),
            new Line(Speaker.Traveler, "谁在那里！？"),
            new Line(Speaker.Fairy, "别害怕别害怕……我不是故意吓你的。"),
            new Line(Speaker.Traveler, "你是什么？"),
            new Line(Speaker.Fairy, "我也不知道，现在的我还算不算一个“生命”。"),
            new Line(Speaker.Fairy, "很久以前，我只是浮光宝石里的一缕灵识。"),
            new Line(Speaker.Fairy, "我听得见小镇上的笑声，也能看见所有颜色从宝石中流向街道、花朵和每一扇亮着灯的窗户。"),
            new Line(Speaker.Fairy, "那时候，浮光小镇不是现在这个样子。"),
            new Line(Speaker.Traveler, "那……后来发生了什么？"),
            new Line(Speaker.Fairy, "宝石碎了……"),
            new Line(Speaker.Fairy, "七道光从宝石中飞散出去，落在了小镇不同的地方。"),
            new Line(Speaker.Fairy, "红色、橙色、黄色、绿色、青色、蓝色，还有紫色……"),
            new Line(Speaker.Fairy, "它们散落以后，小镇一点点变成了灰白。"),
            new Line(Speaker.Traveler, "这里的人呢？"),
            new Line(Speaker.Fairy, "他们都离开了。"),
            new Line(Speaker.Fairy, "一开始，大家也试过很多办法。"),
            new Line(Speaker.Fairy, "他们重新粉刷房屋，种下新的花朵，在森林点起整夜不灭的篝火……"),
            new Line(Speaker.Fairy, "可无论带回来什么，进入小镇以后，都会失去原本的颜色。"),
            new Line(Speaker.Fairy, "他们记得这里曾经有多美。"),
            new Line(Speaker.Fairy, "也正因为记得，才更无法忍受它现在的样子。"),
            new Line(Speaker.Fairy, "后来，离开的人越来越多。"),
            new Line(Speaker.Fairy, "直到最后一扇窗户，也不再亮起灯光。"),
            new Line(Speaker.Traveler, "那你为什么还保留着颜色？"),
            new Line(Speaker.Fairy, "因为我原本就是宝石的一部分。"),
            new Line(Speaker.Fairy, "宝石破碎时，我被最后一束光带了出来。"),
            new Line(Speaker.Fairy, "可我没有真正的身体，也无法离开碎片的力量太久。"),
            new Line(Speaker.Fairy, "我能感觉到它们在哪里，却碰不到它们，更没办法把它们恢复。"),
            new Line(Speaker.Fairy, "所以，我只能躲在森林里。"),
            new Line(Speaker.Fairy, "每天看着自己的光变得更弱一点。"),
            new Line(Speaker.Traveler, "那封信，是你送来的？"),
            new Line(Speaker.Fairy, "是我。"),
            new Line(Speaker.Fairy, "我把剩下的一点光藏进信纸里，让它顺着风离开小镇。"),
            new Line(Speaker.Fairy, "我不知道它会落到谁的手里，也不知道收到它的人是否愿意相信。"),
            new Line(Speaker.Fairy, "我等了很久……"),
            new Line(Speaker.Fairy, "直到你真的来了。"),
            new Line(Speaker.Traveler, "为什么选择我？"),
            new Line(Speaker.Fairy, "其实，不是我选择了你。"),
            new Line(Speaker.Fairy, "是你读完那封信以后，选择来到这里。"),
            new Line(Speaker.Fairy, "以前也有人经过小镇。"),
            new Line(Speaker.Fairy, "可他们看见这里的灰白后，很快就离开了。"),
            new Line(Speaker.Fairy, "只有你停了下来，还走进了这片森林。"),
            new Line(Speaker.Fairy, "所以我想，也许你愿意听完一个无处可去的精灵……最后的请求……"),
            new Line(Speaker.Fairy, "旅行家，你愿意帮我找回七块宝石碎片吗？"),
            new Line(Speaker.Fairy, "如果能让它们重新回到一起，浮光小镇或许就能再次拥有颜色。"),
            new Line(Speaker.Fairy, "那些离开的人，也许有一天，还会愿意回到这里。"),
            new Line(Speaker.Traveler, "我会帮你找回它们。"),
            new Line(Speaker.Fairy, "真的？"),
            new Line(Speaker.Fairy, "……谢谢你。"),
            new Line(Speaker.Fairy, "请让我跟随你一起寻找吧。"),
            new Line(Speaker.Fairy, "我没有多少力量，但只要靠近宝石碎片，我就能感觉到它发出的光。"),
            new Line(Speaker.Narrator, "浮光精灵缓缓飞到旅行家身边，原本微弱的蓝光稍稍明亮了一些。", true),
            new Line(Speaker.Narrator, "旅行家，我们出发吧。", true),
            new Line(Speaker.Narrator, "在浮光小镇彻底变成一座无人记得的空城之前。", true)
        };
    }
}
