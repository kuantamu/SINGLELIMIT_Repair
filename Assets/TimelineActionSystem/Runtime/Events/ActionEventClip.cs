using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public enum ActionEventType
{
    PlayVfx,
    PlaySfx,
    AttackWindow,
    ComboWindow,
    ApplyBuff   // 自分にバフ・デバフを付与(クリップの開始時に1回)
}

public sealed class ActionEventClip : PlayableAsset
{
    public ActionEventType eventType;

    [Header("VFX")]
    public GameObject effectPrefab;

    [Min(0.01f)]
    public float effectDuration = 2f;

    public Vector3 positionOffset;
    public Vector3 rotationOffset;
    public Vector3 scaleMultiplier = Vector3.one;

    public bool stopWhenClipEnds = true;

    [Header("SFX")]
    public AudioClip audioClip;

    [Header("攻撃判定")]
    public string attackId;

    [Tooltip("AttackWindow のとき、クリップの開始〜終了の間だけ有効になる Box 判定")]
    public AttackHitSettings attackHit = new AttackHitSettings();

    [Header("自分へのバフ・デバフ")]
    [Tooltip("ApplyBuff のとき、クリップの開始時に自分へ付与する(種類・層数・秒数は攻撃判定のバフと同じ設定)")]
    public List<BuffGrant> selfBuffs = new List<BuffGrant>();

    // Timeline 上での初期クリップ長として使用する。
    public override double duration =>
        Mathf.Max(0.01f, effectDuration);

    public override Playable CreatePlayable(
        PlayableGraph graph,
        GameObject owner)
    {
        var playable =
            ScriptPlayable<ActionEventBehaviour>.Create(graph);

        var behaviour = playable.GetBehaviour();

        behaviour.eventType = eventType;
        behaviour.effectPrefab = effectPrefab;
        behaviour.effectDuration = effectDuration;
        behaviour.positionOffset = positionOffset;
        behaviour.rotationOffset = rotationOffset;
        behaviour.scaleMultiplier = scaleMultiplier;
        behaviour.stopWhenClipEnds = stopWhenClipEnds;
        behaviour.audioClip = audioClip;
        behaviour.attackId = attackId;
        behaviour.attackHit = attackHit;
        behaviour.selfBuffs = selfBuffs;

        return playable;
    }
}

public sealed class ActionEventBehaviour : PlayableBehaviour
{
    public ActionEventType eventType;

    public GameObject effectPrefab;
    public float effectDuration;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;
    public Vector3 scaleMultiplier;
    public bool stopWhenClipEnds;

    public AudioClip audioClip;
    public string attackId;

    // 攻撃判定の設定と、このクリップが生成した判定
    public AttackHitSettings attackHit;
    public ActionEventReceiver.AttackBox spawnedAttack;

    // 自分に付与するバフ・デバフ
    public List<BuffGrant> selfBuffs;

    public GameObject spawnedEffect;
    public bool wasActive;
}
