using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 【受ける側の判定】
/// このコライダーに HitBox が触れると、持ち主の CharactorStatus がダメージを受けます。
///
/// ■ 使い方
///  1. キャラのコライダーがあるオブジェクトに、このコンポーネントを付ける
///  2. キャラの親オブジェクトに CharactorStatus があれば自動で見つかる
///  3. 頭・背中など部位ごとにコライダーが分かれている場合は、
///     それぞれに HurtBox を付けて Damage Multiplier を変えると、弱点などを作れる
///
/// ■ 注意
///  HitBox は「触れたコライダーと同じオブジェクト」の HurtBox を探します。
///  コライダーと HurtBox は同じ GameObject に付けてください。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class HurtBox : MonoBehaviour
{
    [Header("ダメージを受けるキャラ(未設定なら親から探す)")]
    [SerializeField] private CharactorStatus status;

    [Header("この部位のダメージ倍率(頭=2.0、硬い部位=0.5 など)")]
    [SerializeField] private float damageMultiplier = 1f;

    [Header("この部位でバフ・デバフを受けるか")]
    [SerializeField] private bool receiveBuffs = true;

    /// <summary>ダメージを受けるキャラ</summary>
    public CharactorStatus Status => status;

    /// <summary>この部位のダメージ倍率</summary>
    public float DamageMultiplier => damageMultiplier;

    /// <summary>
    /// 攻撃を受けたときに呼ばれるイベント(被弾エフェクト・SE・ヒットストップ用)。
    /// 引数は「この HurtBox」と「倍率適用後のダメージ情報」。
    /// </summary>
    public event Action<HurtBox, DamageInfo> OnHurt;

    private void Awake()
    {
        if (status == null) status = GetComponentInParent<CharactorStatus>();
    }

    /// <summary>
    /// 攻撃を受ける。HitBox から呼ばれるが、レイキャストなど別の判定から直接呼んでもよい。
    /// </summary>
    /// <param name="info">ダメージ情報(部位の倍率はここで掛かる)</param>
    /// <param name="buffs">付与するバフ・デバフ(なければ null)</param>
    /// <param name="attacker">攻撃者(いなければ null)</param>
    /// <returns>ダメージが通ったか(無敵なら false)</returns>
    public bool ReceiveHit(DamageInfo info, IEnumerable<BuffGrant> buffs = null, CharactorStatus attacker = null)
    {
        if (status == null) return false;

        // 無敵かどうかは TakeDamage の前に確認しておく
        bool invincible = status.statusBase.HitState == HitState.Invincible;

        // 部位の倍率を掛けてダメージを受ける(無敵なら Miss 表示)
        info.AttackPower *= damageMultiplier;
        status.TakeDamage(info);
        OnHurt?.Invoke(this, info);

        // 無敵のときはバフも入らない
        if (invincible) return false;

        if (receiveBuffs && buffs != null)
        {
            foreach (BuffGrant grant in buffs)
            {
                if (attacker != null) attacker.GiveBuff(status, grant);
                else status.AddBuff(grant.Type, grant.Stacks, grant.GetDuration());
            }
        }
        return true;
    }
}
