using System;
using UnityEngine;

/// <summary>
/// クリップの再生終了方式
/// </summary>
public enum AnimationClipEndMode
{
    Once,
    Loop
}

/// <summary>
/// Once再生が終了した後の処理
/// </summary>
public enum AnimationClipCompletionMode
{
    Hold,
    FadeOut
}

/// <summary>
/// クリップのウェイト制御方式
/// </summary>
public enum AnimationClipWeightMode
{
    Auto,
    Manual
}

/// <summary>
/// ウェイトカーブの評価範囲
/// </summary>
public enum AnimationCurveEvaluationMode
{
    WholeClip,
    BlendRegion
}

[Serializable]
public sealed class AnimationClipSettings
{
    [Header("再生設定")]
    public AnimationClipEndMode endMode =
        AnimationClipEndMode.Once;

    [Min(0.01f)]
    public float speed = 1f;

    [Header("終了後の処理")]
    public AnimationClipCompletionMode completionMode =
        AnimationClipCompletionMode.Hold;

    [Min(0f)]
    public float fadeOutDuration = 0.15f;

    [Header("ウェイト設定")]
    public AnimationClipWeightMode weightMode =
        AnimationClipWeightMode.Auto;

    [Range(0f, 1f)]
    public float manualWeight = 1f;

    public AnimationCurve weightCurve =
        AnimationCurve.Linear(0f, 1f, 1f, 1f);

    public AnimationCurveEvaluationMode curveEvaluationMode =
        AnimationCurveEvaluationMode.WholeClip;

    [Header("個別トランジション")]
    public bool useCustomTransition;

    [Min(0f)]
    public float blendInDuration = 0.15f;

    [Min(0f)]
    public float blendOutDuration = 0.15f;

    public AnimationCurve blendInCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public AnimationCurve blendOutCurve =
        AnimationCurve.Linear(0f, 1f, 1f, 0f);
}