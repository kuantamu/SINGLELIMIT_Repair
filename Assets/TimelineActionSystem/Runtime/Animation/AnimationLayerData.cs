using System;
using UnityEngine;

/// <summary>
/// レイヤーの合成方式
/// </summary>
public enum LayerBlendMode
{
    Override,
    Additive
}

/// <summary>
/// レイヤーウェイトの制御方式
/// </summary>
public enum LayerWeightMode
{
    Fixed,
    Curve
}

[Serializable]
public sealed class AnimationLayerData
{
    [Header("基本設定")]
    public string layerName = "New Layer";

    [Min(0)]
    public int priority = 0;

    [Header("ブレンド設定")]
    public LayerBlendMode blendMode = LayerBlendMode.Override;

    [Range(0f, 1f)]
    public float weight = 1f;

    public LayerWeightMode weightMode = LayerWeightMode.Fixed;

    [Header("ウェイトカーブ")]
    public AnimationCurve weightCurve =
        AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("適用範囲")]
    public AvatarMask avatarMask;
}