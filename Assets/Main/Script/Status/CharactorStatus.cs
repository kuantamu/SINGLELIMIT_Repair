using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// キャラクターのステータス・ダメージ・バフ管理の窓口。
/// 他のスクリプトからバフを扱うときは、基本的にこのクラスのメソッドを呼びます。
/// </summary>
[RequireComponent(typeof(DamagePopupGenerator))]
public class CharactorStatus : MonoBehaviour
{
    [SerializeField] public StatusBase statusBase = new StatusBase();

    [SerializeField] bool isEnemy = false;

    /// <summary>敵側のキャラか(HitBox が敵味方の判定に使う)</summary>
    public bool IsEnemy => isEnemy;

    [Header("攻撃が当たったとき、相手に付与するバフ・デバフ")]
    [SerializeField] private List<BuffGrant> onHitBuffs = new List<BuffGrant>();

    // バフ管理(他のAwakeより先に呼ばれても大丈夫なよう、必要になったときに作る)
    private BuffManager buffManager;
    public BuffManager Buffs
    {
        get
        {
            if (buffManager == null) buffManager = new BuffManager(this);
            return buffManager;
        }
    }

    // ポップアップ生成(こちらも必要になったときに取得)
    private DamagePopupGenerator popupGenerator;
    private DamagePopupGenerator Popup
    {
        get
        {
            if (popupGenerator == null) popupGenerator = GetComponent<DamagePopupGenerator>();
            return popupGenerator;
        }
    }

    private void Awake()
    {
        statusBase.CurrentHP = statusBase.MaxHP;
    }

    private void Update()
    {
        // バフ・デバフの時間経過・毒ダメージなどを処理
        Buffs.UpdateEffects(Time.deltaTime);
    }

    // ============================================================
    //  バフ・デバフ:付与
    //
    //  層数 stacks … 付与する層数。最大層数を超えた分は切り捨て
    //  秒数 duration … 付与する秒数
    //                   ・省略(null)  : そのバフの標準秒数
    //                   ・マイナス    : 時間で消えない(永続)
    //                   ・秒数の上限があるバフは、上限までに切り詰められる
    //                   ・上限が0のバフは即時発動(付与の瞬間に1回働いて消える)
    // ============================================================

    /// <summary>
    /// 【自分】にバフ・デバフを付与する。
    /// 例: status.AddBuff(BuffType.AttackUp, 2);         // 2層・標準秒数
    ///     status.AddBuff(BuffType.AttackUp, 2, 8f);     // 2層・8秒
    ///     status.AddBuff(BuffType.AttackUp, 1, -1f);    // 1層・永続
    /// </summary>
    /// <returns>実際に増えた層数</returns>
    public int AddBuff(BuffType type, int stacks = 1, float? duration = null)
    {
        int added = Buffs.AddBuff(type, stacks, duration);

        // 実際に層が増えたときだけポップアップ表示
        // (即時発動の効果は付与後すぐ消えるので、名前は enum から取る)
        if (added > 0) Popup.Buff(type.ToString(), added);
        return added;
    }

    /// <summary>
    /// 【他のキャラ】にバフ・デバフを付与する。自分自身を渡せば自分にも付与できる。
    /// 例: attacker.GiveBuff(enemy, BuffType.Poison, 3);        // 3層・標準秒数
    ///     attacker.GiveBuff(enemy, BuffType.Poison, 3, 10f);   // 3層・10秒
    /// </summary>
    /// <returns>実際に増えた層数(target が null なら 0)</returns>
    public int GiveBuff(CharactorStatus target, BuffType type, int stacks = 1, float? duration = null)
    {
        if (target == null) return 0;
        return target.AddBuff(type, stacks, duration);
    }

    /// <summary>BuffGrant(種類+層数+秒数のセット)で他のキャラに付与する</summary>
    public int GiveBuff(CharactorStatus target, BuffGrant grant)
    {
        return GiveBuff(target, grant.Type, grant.Stacks, grant.GetDuration());
    }

    /// <summary>複数のバフ・デバフをまとめて他のキャラに付与する</summary>
    public void GiveBuffs(CharactorStatus target, IEnumerable<BuffGrant> grants)
    {
        if (target == null || grants == null) return;
        foreach (var grant in grants) GiveBuff(target, grant);
    }

    /// <summary>同じバフ・デバフを複数のキャラにまとめて付与する(範囲効果用)</summary>
    public void GiveBuffToMany(IEnumerable<CharactorStatus> targets, BuffType type, int stacks = 1, float? duration = null)
    {
        if (targets == null) return;
        foreach (var target in targets) GiveBuff(target, type, stacks, duration);
    }

    // ============================================================
    //  バフ・デバフ:解除・確認
    // ============================================================

    /// <summary>指定した種類の層数を減らす(省略で全部消す)。実際に減った層数を返す</summary>
    public int RemoveBuff(BuffType type, int stacks = int.MaxValue) => Buffs.RemoveBuff(type, stacks);

    /// <summary>バフ・デバフをすべて消す</summary>
    public void ClearAllBuffs() => Buffs.ClearAll();

    /// <summary>デバフ(毒など)だけをすべて消す</summary>
    public void ClearDebuffs() => Buffs.ClearDebuffs();

    /// <summary>バフ(攻撃力アップなど)だけをすべて消す</summary>
    public void ClearBuffs() => Buffs.ClearBuffs();

    /// <summary>指定した種類がかかっているか</summary>
    public bool HasBuff(BuffType type) => Buffs.HasBuff(type);

    /// <summary>指定した種類の現在の層数(かかっていなければ 0)</summary>
    public int GetBuffStacks(BuffType type) => Buffs.GetStacks(type);

    /// <summary>指定した種類の残り秒数(かかっていなければ 0、永続なら -1)</summary>
    public float GetBuffRemainingTime(BuffType type) => Buffs.GetRemainingTime(type);

    /// <summary>指定した種類が永続(時間で消えない)状態か</summary>
    public bool IsBuffPermanent(BuffType type) => Buffs.IsPermanent(type);

    // ============================================================
    //  ステータス操作(バフ効果クラスからも使う)
    // ============================================================

    /// <summary>攻撃力を増減する。バフ解除時は逆の値を渡して元に戻す</summary>
    public void AddAttackModifier(float delta)
    {
        statusBase.Attack += delta;
    }

    // ============================================================
    //  攻撃・ダメージ
    // ============================================================

    public void ReceiveDamage(CharactorStatus target)
    {
        CharaAttack(target);
    }

    /// <summary>
    /// 相手を攻撃する。ダメージを与えたあと、
    /// onHitBuffs に設定したバフ・デバフを相手に付与する。
    /// </summary>
    public void CharaAttack(CharactorStatus targetStatus)
    {
        DamageInfo damage = new DamageInfo()
        {
            AttackPower = statusBase.Attack,
            IsCritical = Random.Range(0.0f, 100.0f) < statusBase.CriticalRate,
            CriticalMultiplier = statusBase.CriticalMult
        };
        targetStatus.TakeDamage(damage);

        // 攻撃時に発動する効果へ通知
        Buffs.NotifyAttack();

        // 攻撃が当たったときの付与(インスペクターで設定)
        GiveBuffs(targetStatus, onHitBuffs);
    }

    /// <summary>ダメージをそのまま受ける(毒など、無敵や急所の影響を受けないダメージ用)</summary>
    public void AddDamage(int damage)
    {
        statusBase.CurrentHP -= damage;
        Popup.Damage(damage);
    }

    /// <summary>攻撃によるダメージを受ける(無敵・急所を考慮)</summary>
    public void TakeDamage(DamageInfo damage)
    {
        if (statusBase.HitState == HitState.Invincible)
        {
            Popup.Damage(0);
            return;
        }
        float Critical = damage.IsCritical ? damage.CriticalMultiplier : 1;
        float dam = damage.AttackPower * Critical;
        statusBase.CurrentHP -= dam;
        Popup.Damage((int)dam);
        Debug.Log(statusBase.CurrentHP + "/" + statusBase.MaxHP + "Critical:" + damage.IsCritical);
    }
}
