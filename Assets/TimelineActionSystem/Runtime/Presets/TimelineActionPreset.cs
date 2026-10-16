using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(
    fileName = "NewTimelinePreset",
    menuName = "Timeline Action System/Preset"
)]
public sealed class TimelineActionPreset : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public int id;
        public string actionName;
        public TimelineAsset timeline;

        [Tooltip("待機など、アニメーションをループ再生する")]
        public bool loopAnimation;

        [Tooltip("ブレンド対象のアニメーション")]
        public AnimationClip animationClip;

        [Min(0.01f)]
        public float defaultSpeed = 1f;

        [Min(0f)]
        public float transitionDuration = 0.15f;
    }

    [SerializeField]
    private string presetName = "New Preset";

    [SerializeField]
    private List<Entry> entries = new();

    public string PresetName => presetName;
    public IReadOnlyList<Entry> Entries => entries;
}