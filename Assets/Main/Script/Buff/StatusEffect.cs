using System;
using UnityEngine;

/// <summary>
/// バフ・デバフ効果の基底クラス(ScriptableObjectではなく普通のクラス)。
///
/// ■ 層数(Stacks)
///  ・同じ種類の効果は1つにまとまり、「層数」で強さを表します。
///  ・MaxStacks が正の数 … その層数まで重ねがけできる(超えた分は切り捨て)
///  ・MaxStacks がマイナス … 上限なし(いくらでも重ねられる)
///
/// ■ 秒数(Duration)
///  ・付与するときに秒数を指定できます。省略すると DefaultDuration を使います。
///  ・付与する秒数がマイナス … 時間では消えない(永続)
///  ・MaxDuration が正の数   … 秒数の上限。これを超える秒数は切り詰める
///  ・MaxDuration がマイナス … 秒数の上限なし
///  ・MaxDuration が 0       … 即時発動。付与した瞬間に OnInstant が1回呼ばれ、すぐ消える
///  ・上限(MaxDuration が0以上)があるときは、永続(マイナス秒数)を指定しても上限が優先される
///
/// ■ 新しい効果の作り方
///  このクラスを継承し、必要な On~ メソッドだけ override してください。
/// </summary>
public abstract class StatusEffect
{
    // ───────── 基本情報(読み取り専用)─────────

    /// <summary>効果の種類</summary>
    public BuffType Type { get; private set; }

    /// <summary>表示名</summary>
    public string Name { get; private set; }

    /// <summary>true ならデバフ(状態異常)、false ならバフ</summary>
    public bool IsDebuff { get; private set; }

    /// <summary>重ねがけできる最大層数。マイナスなら上限なし</summary>
    public int MaxStacks { get; private set; }

    /// <summary>秒数の上限。マイナスなら上限なし、0なら即時発動</summary>
    public float MaxDuration { get; private set; }

    /// <summary>付与時に秒数を指定しなかったときの標準秒数(マイナスで永続)</summary>
    public float DefaultDuration { get; private set; }

    /// <summary>層数に上限があるか</summary>
    public bool HasStackLimit => MaxStacks > 0;

    /// <summary>秒数に上限があるか</summary>
    public bool HasDurationLimit => MaxDuration >= 0f;

    /// <summary>即時発動の効果か(付与した瞬間に1回だけ働いて消える)</summary>
    public bool IsInstant => MaxDuration == 0f;

    /// <summary>時間で消えない永続状態か</summary>
    public bool IsPermanent { get; private set; }

    /// <summary>残り時間(秒)。永続の場合は -1 を返す</summary>
    public float RemainingTime => IsPermanent ? -1f : remaining;

    /// <summary>現在の層数</summary>
    public int Stacks { get; private set; }

    /// <summary>この効果を受けているキャラ</summary>
    public CharactorStatus Owner { get; private set; }

    /// <summary>OnInterval が呼ばれる間隔(秒)。0以下なら呼ばれない</summary>
    public float Interval { get; protected set; } = 1f;

    /// <summary>消滅していい状態か(層数0 または 時間切れ)</summary>
    public bool IsExpired => Stacks <= 0 || (!IsPermanent && remaining <= 0f);

    // 実際の残り時間(永続のときは使わない)
    private float remaining = 0f;

    // Interval 用の経過時間(余りを持ち越して誤差を防ぐ)
    private float timer = 0f;

    /// <summary>
    /// 継承先のコンストラクタから呼び、効果の基本設定を渡します。
    /// </summary>
    /// <param name="maxStacks">最大層数(マイナスで上限なし)</param>
    /// <param name="maxDuration">秒数の上限(マイナスで上限なし、0で即時発動)</param>
    /// <param name="defaultDuration">標準の秒数(マイナスで永続)</param>
    protected StatusEffect(BuffType type, string name, bool isDebuff,
                           int maxStacks, float maxDuration, float defaultDuration)
    {
        Type = type;
        Name = name;
        IsDebuff = isDebuff;
        MaxStacks = maxStacks < 0 ? -1 : Mathf.Max(1, maxStacks); // 0 は 1 として扱う
        MaxDuration = maxDuration < 0f ? -1f : maxDuration;
        DefaultDuration = defaultDuration;
    }

    // ───────── BuffManager から呼ばれる内部処理 ─────────

    /// <summary>持ち主を設定して OnApply を呼ぶ(最初の付与時に1回だけ)</summary>
    internal void Initialize(CharactorStatus owner)
    {
        Owner = owner;
        OnApply();
    }

    /// <summary>
    /// 層数を増やし、秒数を反映する。
    /// 秒数は「今の残り時間」と「今回の秒数」の長いほうになる(永続は永続のまま)。
    /// </summary>
    /// <param name="amount">増やす層数</param>
    /// <param name="duration">今回付与する秒数(マイナスで永続)</param>
    /// <returns>実際に増えた層数(最大に達していれば0)</returns>
    internal int AddStacks(int amount, float duration)
    {
        if (amount <= 0) return 0;

        int old = Stacks;

        // オーバーフローしないよう long で足してから上限で切る
        long sum = (long)Stacks + amount;
        long limit = HasStackLimit ? MaxStacks : int.MaxValue;
        Stacks = (int)Math.Min(sum, limit);

        ApplyDuration(duration);

        int added = Stacks - old;
        if (added != 0) OnStacksChanged(old, Stacks);

        // 即時発動:付与した瞬間に1回だけ働く(その後 BuffManager が消す)
        if (IsInstant && Stacks > 0) OnInstant();

        return added;
    }

    /// <summary>層数を減らす。0になると効果は消滅扱いになる</summary>
    /// <returns>実際に減った層数</returns>
    internal int RemoveStacks(int amount)
    {
        if (amount <= 0) return 0;

        int old = Stacks;
        Stacks = Mathf.Max(0, Stacks - amount);

        int removed = old - Stacks;
        if (removed != 0) OnStacksChanged(old, Stacks);
        return removed;
    }

    /// <summary>毎フレーム呼ばれる更新処理</summary>
    internal void Tick(float deltaTime)
    {
        // 一定間隔ごとの処理(毒ダメージなど)
        if (Interval > 0f)
        {
            timer += deltaTime;
            while (timer >= Interval && Stacks > 0)
            {
                timer -= Interval; // 余りを保持
                OnInterval();
            }
        }

        OnUpdate(deltaTime);

        // 残り時間を減らす(永続効果は減らさない)
        if (!IsPermanent) remaining -= deltaTime;
    }

    /// <summary>キャラが攻撃したことを通知する</summary>
    internal void NotifyAttack() => OnAttack();

    /// <summary>効果が消えるときに呼ぶ</summary>
    internal void Remove() => OnRemove();

    // ───────── 秒数の計算 ─────────

    /// <summary>
    /// 指定された秒数を、上限や永続のルールに合わせて残り時間へ反映する。
    /// </summary>
    private void ApplyDuration(float requested)
    {
        float d;

        if (IsInstant)
        {
            d = 0f;                              // 即時発動は時間を持たない
        }
        else if (requested < 0f)
        {
            // マイナス = 永続。ただし上限があるなら上限が優先
            d = HasDurationLimit ? MaxDuration : -1f;
        }
        else
        {
            d = HasDurationLimit ? Mathf.Min(requested, MaxDuration) : requested;
        }

        if (d < 0f)
        {
            IsPermanent = true;                  // 永続になる
            return;
        }

        if (IsPermanent) return;                 // 永続は短い秒数で上書きしない

        remaining = Mathf.Max(remaining, d);     // 長いほうを採用
    }

    // ───────── 継承先で override するフック ─────────

    /// <summary>最初に付与された瞬間(層数はまだ0)</summary>
    protected virtual void OnApply() { }

    /// <summary>層数が変わったとき(増加・減少どちらでも)</summary>
    protected virtual void OnStacksChanged(int oldStacks, int newStacks) { }

    /// <summary>Interval 秒ごとに呼ばれる</summary>
    protected virtual void OnInterval() { }

    /// <summary>
    /// 即時発動(MaxDuration が 0)の効果が付与された瞬間に1回呼ばれる。
    /// 標準では OnInterval と同じ処理を行う(毒なら1回分のダメージ)。
    /// </summary>
    protected virtual void OnInstant() { OnInterval(); }

    /// <summary>毎フレーム呼ばれる</summary>
    protected virtual void OnUpdate(float deltaTime) { }

    /// <summary>持ち主が攻撃したとき</summary>
    protected virtual void OnAttack() { }

    /// <summary>効果が消える直前(ステータス変更の後始末はここ)</summary>
    protected virtual void OnRemove() { }
}
