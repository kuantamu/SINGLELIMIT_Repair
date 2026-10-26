using System;
using System.Collections.Generic;

/// <summary>
/// 1キャラ分のバフ・デバフを管理するクラス。
/// 普通は CharactorStatus 経由で使い、直接触る必要はありません。
/// </summary>
public class BuffManager
{
    // 持ち主
    private readonly CharactorStatus owner;

    // 現在かかっている効果(同じ種類は1つにまとまる)
    private readonly List<StatusEffect> activeEffects = new List<StatusEffect>();

    // UpdateEffects 実行中かどうか。
    // 更新中にリストから消すとループが壊れるため、更新後にまとめて消す。
    private bool isUpdating = false;

    public BuffManager(CharactorStatus owner)
    {
        this.owner = owner;
    }

    /// <summary>現在かかっている効果の一覧(UI表示用・読み取り専用)</summary>
    public IReadOnlyList<StatusEffect> ActiveEffects => activeEffects;

    // ───────── 付与 ─────────

    /// <summary>
    /// バフ・デバフを付与する。
    /// すでに同じ種類がかかっていれば層数を足す(最大層数があれば頭打ち)。
    /// 秒数は「今の残り時間」と「今回の秒数」の長いほうになる。
    /// </summary>
    /// <param name="type">バフの種類</param>
    /// <param name="stacks">付与する層数(1を複数回付与しても、3を1回付与しても同じ)</param>
    /// <param name="duration">付与する秒数。マイナスで永続、null(省略)で標準秒数</param>
    /// <returns>実際に増えた層数(すでに最大なら0)</returns>
    public int AddBuff(BuffType type, int stacks = 1, float? duration = null)
    {
        if (stacks <= 0) return 0;

        var effect = Find(type);
        if (effect == null)
        {
            // 新規にかかる場合:作って登録し、初期化してから層数を入れる
            effect = BuffFactory.Create(type);
            activeEffects.Add(effect);
            effect.Initialize(owner);
        }

        int added = effect.AddStacks(stacks, duration ?? effect.DefaultDuration);

        // 即時発動の効果はここで消える(更新中は更新後にまとめて消す)
        if (!isUpdating) RemoveExpired();
        return added;
    }

    // ───────── 解除 ─────────

    /// <summary>
    /// 指定した種類の層数を減らす。0になれば効果は消える。
    /// stacks を省略すると、その種類を全部消す。
    /// </summary>
    /// <returns>実際に減った層数</returns>
    public int RemoveBuff(BuffType type, int stacks = int.MaxValue)
    {
        var effect = Find(type);
        if (effect == null || stacks <= 0) return 0;

        int removed = effect.RemoveStacks(stacks);
        if (!isUpdating) RemoveExpired();
        return removed;
    }

    /// <summary>バフ・デバフをすべて消す</summary>
    public void ClearAll() => ClearWhere(e => true);

    /// <summary>デバフ(毒など)だけをすべて消す(状態異常回復用)</summary>
    public void ClearDebuffs() => ClearWhere(e => e.IsDebuff);

    /// <summary>バフ(攻撃力アップなど)だけをすべて消す(バフ解除用)</summary>
    public void ClearBuffs() => ClearWhere(e => !e.IsDebuff);

    // ───────── 問い合わせ ─────────

    /// <summary>指定した種類の効果を取得する(かかっていなければ null)</summary>
    public StatusEffect GetEffect(BuffType type) => Find(type);

    /// <summary>指定した種類が今かかっているか</summary>
    public bool HasBuff(BuffType type) => Find(type) != null;

    /// <summary>指定した種類の現在の層数(かかっていなければ 0)</summary>
    public int GetStacks(BuffType type)
    {
        var effect = Find(type);
        return effect != null ? effect.Stacks : 0;
    }

    /// <summary>
    /// 指定した種類の残り時間(秒)。
    /// かかっていなければ 0、永続なら -1 を返す。
    /// </summary>
    public float GetRemainingTime(BuffType type)
    {
        var effect = Find(type);
        return effect != null ? effect.RemainingTime : 0f;
    }

    /// <summary>指定した種類が永続(時間で消えない)状態か</summary>
    public bool IsPermanent(BuffType type)
    {
        var effect = Find(type);
        return effect != null && effect.IsPermanent;
    }

    // ───────── 毎フレーム処理 ─────────

    /// <summary>
    /// 全効果を更新し、期限切れを取り除く。
    /// CharactorStatus.Update から毎フレーム呼ばれる。
    /// </summary>
    public void UpdateEffects(float deltaTime)
    {
        isUpdating = true;

        // 後ろから回す(更新中に新しい効果が追加されても安全)
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            activeEffects[i].Tick(deltaTime);
        }

        isUpdating = false;
        RemoveExpired();
    }

    /// <summary>攻撃したことを全効果に通知する(攻撃時に発動する効果用)</summary>
    public void NotifyAttack()
    {
        foreach (var effect in activeEffects)
        {
            effect.NotifyAttack();
        }
    }

    // ───────── 内部処理 ─────────

    /// <summary>消滅していない効果を種類で探す</summary>
    private StatusEffect Find(BuffType type)
    {
        foreach (var effect in activeEffects)
        {
            if (effect.Type == type && !effect.IsExpired) return effect;
        }
        return null;
    }

    /// <summary>条件に合う効果の層数をすべて0にして消す</summary>
    private void ClearWhere(Func<StatusEffect, bool> condition)
    {
        foreach (var effect in activeEffects)
        {
            if (condition(effect)) effect.RemoveStacks(int.MaxValue);
        }
        if (!isUpdating) RemoveExpired();
    }

    /// <summary>期限切れの効果を OnRemove を呼んでからリストから外す</summary>
    private void RemoveExpired()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];
            if (!effect.IsExpired) continue;

            activeEffects.RemoveAt(i);
            effect.Remove();
        }
    }
}
