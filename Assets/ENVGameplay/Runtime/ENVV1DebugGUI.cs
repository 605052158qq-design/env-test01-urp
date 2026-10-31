using System.Linq;
using UnityEngine;

/// <summary>
/// Editor-facing V1 control panel. It invokes the same gameplay methods as the
/// real interactions, but needs no temporary scene triggers or travel time.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVV1DebugGUI : MonoBehaviour
{
    public bool visibleInPlayerBuild;
    public bool panelVisible = true;
    public ENVWorldProgress world;
    public ENVRegionTitleUI titleUi;
    public ENVPlayerInteraction player;
    public ENVFairyIntroTrigger fairyTrigger;
    public ENVFairyFollower fairy;
    public ENVEndSequenceOnCompletion ending;

    private readonly string[] regionNames =
    {
        "微光码头", "花园区域", "林地区域", "营地区域", "雪山区域", "终章区域"
    };
    private readonly string[] gemNames =
    {
        "蓝宝石", "红宝石", "黄宝石", "青宝石", "橙宝石", "绿宝石", "紫宝石"
    };

    private void Awake() { ResolveReferences(); }

    private void ResolveReferences()
    {
        if (world == null) world = ENVWorldProgress.Instance ?? FindObjectOfType<ENVWorldProgress>(true);
        if (titleUi == null) titleUi = ENVRegionTitleUI.Instance ?? FindObjectOfType<ENVRegionTitleUI>(true);
        if (player == null) player = FindObjectOfType<ENVPlayerInteraction>(true);
        if (fairyTrigger == null) fairyTrigger = FindObjectOfType<ENVFairyIntroTrigger>(true);
        if (fairy == null) fairy = FindObjectOfType<ENVFairyFollower>(true);
        if (ending == null) ending = FindObjectOfType<ENVEndSequenceOnCompletion>(true);
    }

    private void OnGUI()
    {
        if (!Application.isEditor && !visibleInPlayerBuild) return;
        ENVStartScreen start = FindObjectOfType<ENVStartScreen>(true);
        if (start != null && start.menuRoot != null && start.menuRoot.activeInHierarchy) return;

        float width = 258f;
        Rect toggleRect = new Rect(Screen.width - width - 18f, 18f, width, 30f);
        if (GUI.Button(toggleRect, panelVisible ? "隐藏 V1 调试面板" : "显示 V1 调试面板"))
            panelVisible = !panelVisible;
        if (!panelVisible) return;

        ResolveReferences();
        Rect panel = new Rect(Screen.width - width - 18f, 56f, width, 582f);
        GUI.Box(panel, "V1 逻辑调试（不使用场景触发物）");
        GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 28f, panel.width - 24f, panel.height - 38f));
        DrawGemControls();
        GUILayout.Space(8f);
        GUILayout.Label("区域标题");
        for (int i = 0; i < regionNames.Length; i++)
        {
            string title = regionNames[i];
            if (GUILayout.Button("显示：" + title)) ShowRegion(title);
        }
        GUILayout.Space(8f);
        if (GUILayout.Button("测试：坐下 / 起身"))
        {
            if (player != null) player.DebugToggleSeat();
        }
        if (GUILayout.Button("测试：触摸动作"))
        {
            ENVPlayerVisualAnimator visual = player != null ? player.GetComponent<ENVPlayerVisualAnimator>() : null;
            if (visual != null) visual.PlayTouch();
        }
        if (GUILayout.Button("测试：古树 / 精灵"))
        {
            if (fairyTrigger != null) fairyTrigger.DebugActivate();
            else if (fairy != null)
            {
                fairy.BeginFollow();
                if (titleUi != null) titleUi.Show("精灵出现 · 正在跟随", 2.5f);
            }
            else if (titleUi != null) titleUi.Show("精灵资源尚未导入", 2.5f);
        }
        if (GUILayout.Button("完成全部宝石（测试结局）")) ActivateThrough(int.MaxValue);
        if (GUILayout.Button("重置宝石流程") && world != null) world.ResetProgress();
        GUILayout.EndArea();
    }

    private void DrawGemControls()
    {
        GUILayout.Label("宝石流程");
        ENVGem next = world != null ? world.NextGem : null;
        GUILayout.Label(next != null ? "下一颗：" + next.gemstoneName : "宝石流程已完成");
        if (GUILayout.Button("激活下一颗宝石")) ActivateThrough(next != null ? next.sequenceIndex : -1);
        for (int i = 0; i < gemNames.Length; i++)
        {
            int target = i;
            if (GUILayout.Button("激活至：" + gemNames[i])) ActivateThrough(target);
        }
    }

    private void ActivateThrough(int targetIndex)
    {
        if (world == null) return;
        foreach (ENVGem gem in world.Gems.Where(g => g != null).OrderBy(g => g.sequenceIndex))
        {
            if (gem.sequenceIndex > targetIndex || gem.Activated) continue;
            if (!gem.TryActivate()) break;
        }
    }

    private void ShowRegion(string name)
    {
        if (titleUi != null) titleUi.Show(name, 2.4f);
    }
}
