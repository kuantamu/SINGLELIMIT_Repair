using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[RequireComponent(typeof(PlayableDirector))]
public sealed class TimelineActionPlayer : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private ActionEventReceiver eventReceiver;
    [SerializeField] private List<TimelineActionPreset> presets = new();

    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private readonly AnimationClipPlayable[] clips =
        new AnimationClipPlayable[2];

    private PlayableDirector eventDirector;

    private int currentSlot = -1;
    private int targetSlot = -1;
    private bool transitioning;
    private float transitionElapsed;
    private float transitionDuration;

    private float eventElapsed;
    private float eventSpeed;
    private bool eventPlaying;

    private TimelineActionPreset playingPreset;
    private TimelineAsset playingTimeline;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (eventReceiver == null)
            eventReceiver = GetComponent<ActionEventReceiver>();

        eventDirector = GetComponent<PlayableDirector>();
        eventDirector.timeUpdateMode = DirectorUpdateMode.Manual;

        if (animator == null)
        {
            Debug.LogError("Animator が必要です。", this);
            enabled = false;
            return;
        }

        graph = PlayableGraph.Create($"{name}_ActionGraph");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

        mixer = AnimationMixerPlayable.Create(graph, 2);

        var output = AnimationPlayableOutput.Create(
            graph,
            "Action Animation",
            animator
        );

        output.SetSourcePlayable(mixer);
        mixer.SetInputWeight(0, 0f);
        mixer.SetInputWeight(1, 0f);

        graph.Play();
    }

    public bool Play(string presetName, int entryId)
    {
        var preset = FindPreset(presetName);
        if (preset == null) return false;

        foreach (var entry in preset.Entries)
        {
            if (entry.id == entryId)
                return PlayEntry(
                    preset,
                    entry,
                    entry.defaultSpeed,
                    entry.transitionDuration
                    );
        }

        Debug.LogWarning($"Timeline ID が見つかりません: {entryId}", this);
        return false;
    }

    public bool Play(
        string presetName,
        string actionName,
        float speed = -1f,
        float transition = -1f)
    {
        var preset = FindPreset(presetName);
        if (preset == null) return false;

        foreach (var entry in preset.Entries)
        {
            if (entry.actionName != actionName)
                continue;

            float actualSpeed = speed > 0f
                ? speed
                : entry.defaultSpeed;

            float actualTransition = transition >= 0f
                ? transition
                : entry.transitionDuration;

            return PlayEntry(
                preset,
                entry,
                actualSpeed,
                actualTransition
                );
        }

        Debug.LogWarning($"Timeline が見つかりません: {actionName}", this);
        return false;
    }

    private TimelineActionPreset FindPreset(string presetName)
    {
        foreach (var preset in presets)
        {
            if (preset != null && preset.PresetName == presetName)
                return preset;
        }

        Debug.LogWarning($"プリセットが見つかりません: {presetName}", this);
        return null;
    }

    private bool PlayEntry(
    TimelineActionPreset preset,
    TimelineActionPreset.Entry entry,
    float speed,
    float duration)
    {
        //null チェック
        if (entry == null || entry.animationClip == null)
        {
            Debug.LogWarning("AnimationClip が未設定です。", this);
            return false;
        }

        // 同じプリセットの同じ Timeline が再生中なら無視する。
        if (eventPlaying &&
            playingPreset == preset &&
            entry.timeline != null &&
            playingTimeline == entry.timeline)
        {
            return false;
        }

        // 遷移中に新しい要求が来た場合は、重みの大きい側を採用する。
        if (transitioning)
        {
            currentSlot =
                mixer.GetInputWeight(targetSlot) >=
                mixer.GetInputWeight(currentSlot)
                    ? targetSlot
                    : currentSlot;

            transitioning = false;
            targetSlot = -1;

            mixer.SetInputWeight(currentSlot, 1f);
            mixer.SetInputWeight(1 - currentSlot, 0f);
        }

        StopEventTimeline();

        targetSlot = currentSlot < 0 ? 0 : 1 - currentSlot;
        ClearSlot(targetSlot);

        clips[targetSlot] = AnimationClipPlayable.Create(
            graph,
            entry.animationClip
        );

        clips[targetSlot].SetSpeed(Mathf.Max(0.01f, speed));
        clips[targetSlot].SetApplyFootIK(false);
        clips[targetSlot].SetApplyPlayableIK(false);

        graph.Connect(clips[targetSlot], 0, mixer, targetSlot);
        mixer.SetInputWeight(targetSlot, 0f);

        eventElapsed = 0f;
        eventSpeed = Mathf.Max(0.01f, speed);

        if (entry.timeline != null)
        {
            StartEventTimeline(entry.timeline, preset);
        }

        if (currentSlot < 0 || duration <= 0f)
        {
            if (currentSlot >= 0)
                mixer.SetInputWeight(currentSlot, 0f);

            mixer.SetInputWeight(targetSlot, 1f);
            currentSlot = targetSlot;
            targetSlot = -1;
            transitioning = false;
        }
        else
        {
            transitionElapsed = 0f;
            transitionDuration = duration;
            transitioning = true;
        }

        return true;
    }

    // Timeline の再生を開始する。
    private void StartEventTimeline(
    TimelineAsset asset,
    TimelineActionPreset preset)
    {
        eventDirector.Stop();
        eventDirector.playableAsset = asset;
        eventDirector.RebuildGraph();

        foreach (var track in asset.GetOutputTracks())
        {
            if (track is ActionEventTrack)
            {
                eventDirector.SetGenericBinding(track, eventReceiver);
            }
        }

        eventDirector.Play();
        eventDirector.time = 0d;
        eventDirector.Evaluate();

        playingPreset = preset;
        playingTimeline = asset;
        eventPlaying = true;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (transitioning)
        {
            transitionElapsed += dt;

            float t = Mathf.Clamp01(
                transitionElapsed / transitionDuration
            );

            mixer.SetInputWeight(currentSlot, 1f - t);
            mixer.SetInputWeight(targetSlot, t);

            if (t >= 1f)
            {
                mixer.SetInputWeight(currentSlot, 0f);
                currentSlot = targetSlot;
                targetSlot = -1;
                transitioning = false;
            }
        }

        // アニメーションと Timeline を同じ経過時間で進める。
        if (eventPlaying)
        {
            eventElapsed += dt * eventSpeed;

            double timelineDuration =
                eventDirector.playableAsset.duration;

            eventDirector.time = System.Math.Min(
                eventElapsed,
                timelineDuration
            );

            eventDirector.Evaluate();

            if (eventElapsed >= timelineDuration)
                StopEventTimeline();
        }

        if (graph.IsValid())
            graph.Evaluate(dt);
    }

    // Timeline の再生を停止する。
    private void StopEventTimeline()
    {
        if (eventDirector != null)
        {
            eventDirector.Stop();
        }

        eventPlaying = false;
        playingPreset = null;
        playingTimeline = null;

        if (eventReceiver != null)
        {
            eventReceiver.ResetAllAttackWindows();
        }
    }

    private void ClearSlot(int slot)
    {
        if (clips[slot].IsValid())
        {
            if (graph.IsValid() && graph.GetRootPlayableCount() >= 0)
                graph.Disconnect(mixer, slot);

            clips[slot].Destroy();
        }

        mixer.SetInputWeight(slot, 0f);
    }

    public void Stop()
    {
        StopEventTimeline();

        if (!graph.IsValid()) return;

        mixer.SetInputWeight(0, 0f);
        mixer.SetInputWeight(1, 0f);

        currentSlot = -1;
        targetSlot = -1;
        transitioning = false;
    }

    private void OnDestroy()
    {
        StopEventTimeline();

        if (graph.IsValid())
            graph.Destroy();
    }
}