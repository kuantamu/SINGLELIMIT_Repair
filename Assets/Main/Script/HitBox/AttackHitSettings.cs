using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Timeline の AttackWindow クリップに設定する、攻撃判定(Box)の設定。
/// クリップの開始で判定が生成され、クリップの終了で消えます。
///
/// 位置・向きは、ActionEventReceiver の Attack Root(未設定なら Effect Root、
/// それも無ければレシーバー自身)を基準にしたローカル値です。
/// これはエフェクトの positionOffset / rotationOffset と同じ仕様です。
/// </summary>
[Serializable]
public sealed class AttackHitSettings
{
    [Header("判定の形(Box)")]
    [Tooltip("基準 Transform からの位置(ローカル)")]
    public Vector3 positionOffset = new Vector3(0f, 1f, 1f);

    [Tooltip("基準 Transform からの向き(オイラー角)")]
    public Vector3 rotationOffset = Vector3.zero;

    [Tooltip("Box の大きさ(幅・高さ・奥行き)。基準 Transform のスケールも掛かる")]
    public Vector3 size = Vector3.one;

    [Header("ダメージ")]
    [Tooltip("OwnerAttack: 攻撃者の攻撃力×倍率 / Fixed: 固定ダメージ")]
    public DamageSource damageSource = DamageSource.OwnerAttack;

    [Tooltip("OwnerAttack: 攻撃力にかける倍率 / Fixed: ダメージ量そのもの")]
    public float damageValue = 1f;

    [Tooltip("当たったときに付与するバフ・デバフ")]
    public List<BuffGrant> buffs = new List<BuffGrant>();

    [Header("当たる相手")]
    public HitTargetType targetType = HitTargetType.OppositeTeam;

    public LayerMask hurtLayers = ~0;

    [Tooltip("同じ相手に再びヒットする間隔(秒)。0以下ならクリップ中は同じ相手に1回だけ")]
    public float rehitInterval = 0f;
}
