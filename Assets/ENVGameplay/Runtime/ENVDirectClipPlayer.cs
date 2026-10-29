using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// Plays a referenced AnimationClip directly on the EXISTING Animator using Unity Playables.
/// No new AnimatorController, new avatar or second set of visual prefabs is created.
/// It is intended for a short single-player interaction, not layered/combat animation.
/// </summary>
public sealed class ENVDirectClipPlayer
{
    private PlayableGraph graph;
    private AnimationClipPlayable clipPlayable;
    private AnimationClip clip;
    private float time;
    private bool loop;
    private bool holdLastFrame;

    public bool IsRunning { get { return graph.IsValid(); } }
    public bool IsAtEnd { get { return clip == null || (!loop && time >= clip.length); } }

    public void Play(Animator animator, AnimationClip animation, bool loopAnimation, bool holdEnd)
    {
        Stop();
        if (animator == null || animation == null) return;
        clip = animation;
        time = 0f;
        loop = loopAnimation;
        holdLastFrame = holdEnd;
        graph = PlayableGraph.Create("ENV Interaction Clip");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        clipPlayable = AnimationClipPlayable.Create(graph, clip);
        clipPlayable.SetApplyFootIK(false);
        var output = AnimationPlayableOutput.Create(graph, "Existing Character Animator", animator);
        output.SetSourcePlayable(clipPlayable);
        graph.Play();
        clipPlayable.SetTime(0d);
        graph.Evaluate(0f);
    }

    public void Tick(float deltaTime)
    {
        if (!graph.IsValid() || clip == null) return;
        time += Mathf.Max(0f, deltaTime);
        float sample = loop ? Mathf.Repeat(time, Mathf.Max(0.01f, clip.length)) : Mathf.Min(time, clip.length);
        clipPlayable.SetTime(sample);
        graph.Evaluate(0f);
        if (!loop && !holdLastFrame && time >= clip.length) Stop();
    }

    public void Stop()
    {
        if (graph.IsValid()) graph.Destroy();
        clip = null;
        time = 0f;
        loop = false;
        holdLastFrame = false;
    }
}
