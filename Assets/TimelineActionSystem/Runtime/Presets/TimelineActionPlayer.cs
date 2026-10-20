using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[RequireComponent(typeof(PlayableDirector))]
public sealed class TimelineActionPlayer : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Animator animator;
    [SerializeField] private ActionEventReceiver eventReceiver;
    [SerializeField] private List<TimelineActionPreset> presets = new();

    [Header("初期デフォルト再生")]
    [SerializeField] private string initialDefaultPreset = "";
    [SerializeField] private string initialDefaultAction = "";

    public event Action<string, TimelineActionPreset.Entry> PlaybackStarted;
    public event Action<string, TimelineActionPreset.Entry> PlaybackEnded;

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

    private TimelineActionPreset defaultPreset;
    private TimelineActionPreset.Entry defaultEntry;

    private TimelineActionPreset activePreset;
    private TimelineActionPreset.Entry activeEntry;
    private bool activeAllowsPlaybackChange = true;
    private bool activeIsDefault;

    private bool playbackChangeAllowed = true;
    private float actionElapsed;
    private float actionDuration;

    private bool eventPlaying;
    private float eventElapsed;
    private float eventSpeed = 1f;

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
            graph, "Action Animation", animator);

        output.SetSourcePlayable(mixer);

        mixer.SetInputWeight(0, 0f);
        mixer.SetInputWeight(1, 0f);

        graph.Play();

        if (!string.IsNullOrWhiteSpace(initialDefaultPreset) &&
            !string.IsNullOrWhiteSpace(initialDefaultAction))
        {
            SetDefaultPlayback(
                initialDefaultPreset,
                initialDefaultAction,
                true);
        }
    }

    // --------------------------------------------------
    // 公開 API
    // --------------------------------------------------

    public bool Play(
        string presetName,
        string actionName,
        float speed = -1f,
        float transition = -1f,
        bool allowPlaybackChange = true)
    {
        if (!TryFindEntry(
                presetName, actionName,
                out var preset, out var entry))
            return false;

        float actualSpeed = speed > 0f
            ? speed : entry.defaultSpeed;

        float actualTransition = transition >= 0f
            ? transition : entry.transitionDuration;

        return PlayEntry(
            preset, entry,
            actualSpeed, actualTransition,
            allowPlaybackChange,
            force: false,
            isDefault: false);
    }

    public bool Play(
        string presetName,
        int entryId,
        float speed = -1f,
        float transition = -1f,
        bool allowPlaybackChange = true)
    {
        var preset = FindPreset(presetName);
        if (preset == null)
            return false;

        foreach (var entry in preset.Entries)
        {
            if (entry.id != entryId)
                continue;

            float actualSpeed = speed > 0f
                ? speed : entry.defaultSpeed;

            float actualTransition = transition >= 0f
                ? transition : entry.transitionDuration;

            return PlayEntry(
                preset, entry,
                actualSpeed, actualTransition,
                allowPlaybackChange,
                force: false,
                isDefault: false);
        }

        Debug.LogWarning($"ID が見つかりません: {entryId}", this);
        return false;
    }

    public bool ForcePlay(
        string presetName,
        string actionName,
        float speed = -1f,
        float transition = -1f,
        bool allowPlaybackChange = true)
    {
        if (!TryFindEntry(
                presetName, actionName,
                out var preset, out var entry))
            return false;

        float actualSpeed = speed > 0f
            ? speed : entry.defaultSpeed;

        float actualTransition = transition >= 0f
            ? transition : entry.transitionDuration;

        return PlayEntry(
            preset, entry,
            actualSpeed, actualTransition,
            allowPlaybackChange,
            force: true,
            isDefault: false);
    }

    public bool ForcePlay(
        string presetName,
        int entryId,
        float speed = -1f,
        float transition = -1f,
        bool allowPlaybackChange = true)
    {
        var preset = FindPreset(presetName);
        if (preset == null)
            return false;

        foreach (var entry in preset.Entries)
        {
            if (entry.id != entryId)
                continue;

            float actualSpeed = speed > 0f
                ? speed : entry.defaultSpeed;

            float actualTransition = transition >= 0f
                ? transition : entry.transitionDuration;

            return PlayEntry(
                preset, entry,
                actualSpeed, actualTransition,
                allowPlaybackChange,
                force: true,
                isDefault: false);
        }

        Debug.LogWarning($"ID が見つかりません: {entryId}", this);
        return false;
    }

    public void SetPlaybackChangeAllowed(bool allowed)
    {
        playbackChangeAllowed = allowed;
    }

    public bool SetDefaultPlayback(
        string presetName,
        string actionName,
        bool playImmediately = true)
    {
        if (!TryFindEntry(
                presetName, actionName,
                out var preset, out var entry))
        {
            Debug.LogWarning(
                "デフォルト再生の設定に失敗しました。 " +
                "プリセット名・アクション名を確認してください。",
                this);
            return false;
        }

        defaultPreset = preset;
        defaultEntry = entry;

        initialDefaultPreset = presetName;
        initialDefaultAction = actionName;

        if (playImmediately)
            return PlayDefault();

        return true;
    }

    public bool PlayDefault()
    {
        if (defaultPreset == null || defaultEntry == null)
        {
            Debug.LogWarning("デフォルト再生が設定されていません。", this);
            return false;
        }

        if (activeIsDefault &&
            activePreset == defaultPreset &&
            activeEntry == defaultEntry)
        {
            return true;
        }

        return PlayEntry(
            defaultPreset,
            defaultEntry,
            defaultEntry.defaultSpeed,
            defaultEntry.transitionDuration,
            playbackChangeAllowed = true,
            force: true,
            isDefault: true);
    }

    public void Stop()
    {
        FinishActiveAction();

        if (!graph.IsValid())
            return;

        mixer.SetInputWeight(0, 0f);
        mixer.SetInputWeight(1, 0f);

        ClearSlot(0);
        ClearSlot(1);

        currentSlot = -1;
        targetSlot = -1;
        transitioning = false;
    }

    // --------------------------------------------------
    // 再生処理
    // --------------------------------------------------

    private bool PlayEntry(
        TimelineActionPreset preset,
        TimelineActionPreset.Entry entry,
        float speed,
        float duration,
        bool allowChange,
        bool force,
        bool isDefault)
    {
        if (preset == null ||
            entry == null ||
            entry.animationClip == null)
        {
            Debug.LogWarning(
                "再生できません。プリセット、エントリ、" +
                "AnimationClip の設定を確認してください。",
                this);
            return false;
        }

        speed = Mathf.Max(0.01f, speed);
        duration = Mathf.Max(0f, duration);

        // 同一プリセット・同一 Timeline の通常再生は無視する。
        // ForcePlay なら同じアクションも再スタートできる。
        if (!force && IsSameAction(preset, entry))
            return false;

        // 現在の再生が変更を許可していない場合は拒否する。
        if (!force &&
            activeEntry != null &&
            (!playbackChangeAllowed ||
             !activeAllowsPlaybackChange))
        {
            return false;
        }

        if (!graph.IsValid())
            return false;

        CollapseTransition();

        // 古いアクションの終了通知とイベントを片付ける。
        FinishActiveAction();

        int nextSlot = currentSlot < 0 ? 0 : 1 - currentSlot;
        ClearSlot(nextSlot);

        clips[nextSlot] = AnimationClipPlayable.Create(
            graph, entry.animationClip);

        clips[nextSlot].SetSpeed(speed);
        clips[nextSlot].SetApplyFootIK(false);
        clips[nextSlot].SetApplyPlayableIK(false);

        if (!graph.Connect(clips[nextSlot], 0, mixer, nextSlot))
        {
            clips[nextSlot].Destroy();
            clips[nextSlot] = default;
            Debug.LogError("AnimationClipPlayable の接続に失敗しました。", this);
            return false;
        }

        mixer.SetInputWeight(nextSlot, 0f);

        activePreset = preset;
        activeEntry = entry;
        activeAllowsPlaybackChange = allowChange;
        activeIsDefault = isDefault;

        actionElapsed = 0f;

        double timelineLength = entry.timeline != null
            ? entry.timeline.duration
            : 0d;

        float clipLength = entry.animationClip.length;
        float totalLength = Mathf.Max(
            clipLength, (float)timelineLength);

        actionDuration = entry.loopAnimation
            ? float.PositiveInfinity
            : Mathf.Max(0.01f, totalLength / speed);

        eventSpeed = speed;

        if (currentSlot < 0 || duration <= 0f)
        {
            if (currentSlot >= 0)
                mixer.SetInputWeight(currentSlot, 0f);

            mixer.SetInputWeight(nextSlot, 1f);

            currentSlot = nextSlot;
            targetSlot = -1;
            transitioning = false;
        }
        else
        {
            targetSlot = nextSlot;
            transitionElapsed = 0f;
            transitionDuration = duration;
            transitioning = true;
        }

        if (entry.timeline != null)
            StartEventTimeline(entry.timeline);
        PlaybackStarted?.Invoke(preset.PresetName, entry);
        return true;
    }

    private bool IsSameAction(
        TimelineActionPreset preset,
        TimelineActionPreset.Entry entry)
    {
        if (activePreset != preset || activeEntry == null)
            return false;

        if (entry.timeline != null)
            return activeEntry.timeline == entry.timeline;

        return activeEntry == entry;
    }

    private void CollapseTransition()
    {
        if (!transitioning)
            return;

        int keepSlot =
            mixer.GetInputWeight(targetSlot) >
            mixer.GetInputWeight(currentSlot)
                ? targetSlot
                : currentSlot;

        int discardSlot = 1 - keepSlot;

        mixer.SetInputWeight(keepSlot, 1f);
        mixer.SetInputWeight(discardSlot, 0f);

        currentSlot = keepSlot;
        targetSlot = -1;
        transitioning = false;

        ClearSlot(discardSlot);
    }

    private void FinishActiveAction()
    {
        if (activeEntry == null)
        {
            StopEventTimeline();
            return;
        }

        string endedPresetName = activePreset.PresetName;
        var endedEntry = activeEntry;

        StopEventTimeline();

        activePreset = null;
        activeEntry = null;
        activeAllowsPlaybackChange = true;
        activeIsDefault = false;

        PlaybackEnded?.Invoke(endedPresetName, endedEntry);
    }

    // --------------------------------------------------
    // Timeline イベント再生
    // --------------------------------------------------

    private void StartEventTimeline(TimelineAsset asset)
    {
        StopEventTimeline();

        eventDirector.playableAsset = asset;
        eventDirector.RebuildGraph();

        foreach (var track in asset.GetOutputTracks())
        {
            if (track is ActionEventTrack)
            {
                eventDirector.SetGenericBinding(
                    track, eventReceiver);
            }
        }

        eventElapsed = 0f;
        eventDirector.time = 0d;
        eventDirector.Play();
        eventDirector.Evaluate();

        eventPlaying = true;
    }

    private void StopEventTimeline(bool stopVfx = true)
    {
        if (eventDirector != null)
            eventDirector.Stop();

        eventPlaying = false;
        eventElapsed = 0f;

        if (eventReceiver != null)
        {
            eventReceiver.ResetAllAttackWindows();

            if (stopVfx)
                eventReceiver.StopAllVfx();
        }
    }

    private void Update()
    {
        if (!graph.IsValid())
            return;

        float dt = Time.deltaTime;

        if (transitioning)
        {
            transitionElapsed += dt;

            float t = Mathf.Clamp01(
                transitionElapsed / transitionDuration);

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

        if (eventPlaying)
        {
            eventElapsed += dt * eventSpeed;

            double length = eventDirector.playableAsset.duration;
            eventDirector.time = Math.Min(eventElapsed, length);
            eventDirector.Evaluate();

            // イベント Timeline が終了したら自動停止する。
            if (eventElapsed >= length)
                StopEventTimeline(stopVfx: false);
        }

        if (activeEntry != null &&
            !activeEntry.loopAnimation)
        {
            actionElapsed += dt;

            if (actionElapsed >= actionDuration)
            {
                bool wasDefault = activeIsDefault;

                FinishActiveAction();

                // 通常アクション終了時はデフォルト再生に戻る。
                // デフォルト自身が終了した場合は再帰再生しない。
                if (!wasDefault)
                    PlayDefault();
            }
        }

        graph.Evaluate(dt);
    }

    // 検索・破棄

    private TimelineActionPreset FindPreset(string presetName)
    {
        foreach (var preset in presets)
        {
            if (preset != null &&
                preset.PresetName == presetName)
                return preset;
        }

        Debug.LogWarning(
            $"プリセットが見つかりません: {presetName}", this);
        return null;
    }

    private bool TryFindEntry(
        string presetName,
        string actionName,
        out TimelineActionPreset preset,
        out TimelineActionPreset.Entry entry)
    {
        preset = FindPreset(presetName);
        entry = null;

        if (preset == null)
            return false;

        foreach (var candidate in preset.Entries)
        {
            if (candidate != null &&
                candidate.actionName == actionName)
            {
                entry = candidate;
                return true;
            }
        }

        Debug.LogWarning(
            $"アクションが見つかりません: {actionName}", this);
        return false;
    }

    private void ClearSlot(int slot)
    {
        if (clips[slot].IsValid())
        {
            if (graph.IsValid())
                graph.Disconnect(mixer, slot);

            clips[slot].Destroy();
            clips[slot] = default;
        }

        if (mixer.IsValid())
            mixer.SetInputWeight(slot, 0f);
    }

    private void OnDestroy()
    {
        FinishActiveAction();

        if (graph.IsValid())
            graph.Destroy();
    }
}