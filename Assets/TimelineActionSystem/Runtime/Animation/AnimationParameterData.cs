using System;
using UnityEngine;

/// <summary>
/// パラメータの型
/// </summary>
public enum AnimationParameterType
{
    Float,
    Int,
    Bool,
    Trigger
}

/// <summary>
/// パラメータの値変更方式
/// </summary>
public enum AnimationParameterUpdateMode
{
    Immediate,
    Smooth
}

/// <summary>
/// 再生切り替え時のパラメータ処理
/// </summary>
public enum AnimationParameterResetMode
{
    Reset,
    Keep
}

[Serializable]
public sealed class AnimationParameterData
{
    [Header("基本設定")]
    public string parameterName = "New Parameter";

    public AnimationParameterType type =
        AnimationParameterType.Float;

    [Header("初期値")]
    public float defaultFloat;

    public int defaultInt;

    public bool defaultBool;

    [Header("更新設定")]
    public AnimationParameterUpdateMode updateMode =
        AnimationParameterUpdateMode.Immediate;

    [Min(0f)]
    public float smoothDuration = 0.15f;

    public AnimationCurve smoothCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("再生切り替え時")]
    public AnimationParameterResetMode resetMode =
        AnimationParameterResetMode.Reset;
}