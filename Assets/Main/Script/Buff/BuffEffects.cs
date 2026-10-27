using UnityEngine;

// ============================================================
// 個別のバフ・デバフ効果をここに書きます。
// 各クラスの定数で、最大層数・秒数の上限・標準秒数・1層あたりの強さを調整できます。
//
//   最大層数     … マイナスで上限なし
//   秒数の上限   … マイナスで上限なし / 0 で即時発動
//   標準秒数     … 秒数を指定せず付与したときの秒数(マイナスで永続)
// ============================================================

/// <summary>
/// 毒(デバフ)
/// 1秒ごとに「1層あたりのダメージ × 層数」を受けます。
/// 即時発動(秒数の上限を0)にすると、付与した瞬間に1回分のダメージを与えて消えます。
/// </summary>
public class Poison : StatusEffect
{
    private const int MaxStack = 10;               // 最大層数(マイナスで上限なし)
    private const float MaxDurationSec = 30f;      // 秒数の上限(マイナスで上限なし / 0で即時)
    private const float DefaultDurationSec = 5f;   // 標準の秒数(マイナスで永続)
    private const float DamagePerStack = 5f;       // 1層あたりの毒ダメージ

    public Poison() : base(BuffType.Poison, "Poison", true, MaxStack, MaxDurationSec, DefaultDurationSec)
    {
        Interval = 1f; // 1秒ごとにダメージ
    }

    // 1秒ごと(即時発動のときは付与した瞬間)に呼ばれる
    protected override void OnInterval()
    {
        // 層数に比例したダメージを与える
        int damage = Mathf.RoundToInt(DamagePerStack * Stacks);
        Owner.AddDamage(damage);
    }
}

/// <summary>
/// 攻撃力アップ(バフ)
/// 層数 × 1層あたりの値だけ、持ち主の攻撃力を上げます。
/// 効果が切れたら、上げた分をきちんと元に戻します。
/// </summary>
public class AttackUp : StatusEffect
{
    private const int MaxStack = 5;                // 最大層数(マイナスで上限なし)
    private const float MaxDurationSec = 60f;      // 秒数の上限(マイナスで上限なし / 0で即時)
    private const float DefaultDurationSec = 10f;  // 標準の秒数(マイナスで永続)
    private const float AttackPerStack = 5f;       // 1層あたりの攻撃力上昇

    // 今この効果で上乗せしている攻撃力の合計(解除時に引く分)
    private float appliedBonus = 0f;

    public AttackUp() : base(BuffType.AttackUp, "AttackUp", false, MaxStack, MaxDurationSec, DefaultDurationSec)
    {
        Interval = 0f; // 一定間隔の処理は不要
    }

    // 層数が増減したとき、その差の分だけ攻撃力を加減算する
    protected override void OnStacksChanged(int oldStacks, int newStacks)
    {
        float delta = (newStacks - oldStacks) * AttackPerStack;
        Owner.AddAttackModifier(delta);
        appliedBonus += delta;
    }

    // 攻撃力アップには「1回だけ」の効果がないので、即時発動でも何も起きない
    protected override void OnInstant() { }

    // 消えるとき、上乗せしていた攻撃力を元に戻す
    protected override void OnRemove()
    {
        if (appliedBonus != 0f)
        {
            Owner.AddAttackModifier(-appliedBonus);
            appliedBonus = 0f;
        }
    }
}
