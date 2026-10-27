using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>ダメージの計算元</summary>
public enum DamageSource
{
    OwnerAttack, // 攻撃者の攻撃力 × 倍率(急所判定あり)
    Fixed,       // 固定ダメージ(トラップ・攻撃者なしの判定用。急所なし)
}

/// <summary>どの陣営に当たるか</summary>
public enum HitTargetType
{
    OppositeTeam, // 攻撃者と反対の陣営(敵なら味方側、味方なら敵側)
    SameTeam,     // 攻撃者と同じ陣営(回復・バフ付与の判定用)
    Everyone,     // 全員(自分自身は除く)
}

/// <summary>
/// 【攻撃側の判定】
/// このオブジェクトのコライダーが HurtBox に触れると、ダメージとバフを与えます。
/// 遠距離攻撃(弾・衝撃波)にも、近接攻撃の判定にも使えます。
///
/// ■ 使い方(弾の場合)
///  1. 弾のプレハブに Sphere Collider などを付ける(Is Trigger は自動でONになる)
///  2. この HitBox と ProjectileMover を付ける(Rigidbody は自動で付く)
///  3. 撃つ側で Instantiate した直後に Setup(攻撃者) を呼ぶ
///
/// ■ 使い方(近接の場合)
///  攻撃の判定オブジェクトに付けて、最初は非アクティブにしておき、
///  アニメーションのタイミングで Activate() / Deactivate() を呼ぶ
///
/// ■ 当たらないとき
///  Debug Log をONにすると、触れた相手と「なぜ当たらなかったか」がConsoleに出ます。
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class HitBox : MonoBehaviour
{
    // ───────── インスペクター設定 ─────────

    [Header("攻撃する人(未設定なら親から探す。弾は Setup で渡す)")]
    [SerializeField] private CharactorStatus owner;

    [Header("ダメージ")]
    [SerializeField] private DamageSource damageSource = DamageSource.OwnerAttack;
    [Tooltip("OwnerAttack: 攻撃力にかける倍率 / Fixed: ダメージ量そのもの")]
    [SerializeField] private float damageValue = 1f;

    [Header("当たったときに付与するバフ・デバフ")]
    [SerializeField] private List<BuffGrant> buffs = new List<BuffGrant>();

    [Header("当たる相手")]
    [SerializeField] private HitTargetType targetType = HitTargetType.OppositeTeam;
    [SerializeField] private LayerMask hurtLayers = ~0;

    [Header("ヒット回数")]
    [Tooltip("全体で何回当たったら終わりか(1=単発、2以上=貫通弾、0以下=無制限)")]
    [SerializeField] private int maxHits = 1;
    [Tooltip("同じ相手に再び当たる間隔(秒)。0以下なら同じ相手には1回だけ")]
    [SerializeField] private float rehitInterval = 0f;

    [Header("消滅(弾用)")]
    [Tooltip("最大ヒット回数に達したら自分を破棄する")]
    [SerializeField] private bool destroyOnMaxHits = true;
    [Tooltip("この秒数で自動的に消える(0以下なら消えない)")]
    [SerializeField] private float lifeTime = 0f;
    [Tooltip("壁など。触れたら弾を消す(HurtBox が付いていないコライダー限定)")]
    [SerializeField] private LayerMask obstacleLayers = 0;
    [SerializeField] private bool destroyOnObstacle = true;

    [Header("ヒット時イベント(エフェクト・SE用)")]
    public UnityEvent<HurtBox> onHit;

    [Header("デバッグ")]
    [Tooltip("ONにすると、触れた相手と当たらなかった理由をConsoleに出す")]
    [SerializeField] private bool debugLog = false;

    /// <summary>デバッグログのON/OFF</summary>
    public bool DebugLog
    {
        get => debugLog;
        set => debugLog = value;
    }

    // ───────── 内部状態 ─────────

    private Collider col;

    // 相手ごとの最後にヒットした時刻
    private readonly Dictionary<CharactorStatus, float> hitTimes = new Dictionary<CharactorStatus, float>();

    // 今回の有効期間中のヒット回数
    private int hitCount = 0;

    // ───────── Unity イベント ─────────

    private void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;                       // すり抜けて判定だけ取る

        // トリガー判定には Rigidbody が必要。物理では動かさない設定にする
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // 速い弾のすり抜け対策

        // 近接用:攻撃者が未設定なら親から探す
        if (owner == null) owner = GetComponentInParent<CharactorStatus>();
    }

    private void OnEnable()
    {
        ResetHits(); // 非アクティブ→アクティブで、ヒット履歴をリセット
    }

    private void Start()
    {
        if (lifeTime > 0f) Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other) => Process(other, true);

    // 範囲内に居続ける相手への再ヒット(rehitInterval 用)
    private void OnTriggerStay(Collider other)
    {
        if (rehitInterval > 0f) Process(other, false);
    }

    // ───────── 外から呼べるメソッド ─────────

    /// <summary>攻撃者を設定する(弾を生成した直後に呼ぶ)</summary>
    public void Setup(CharactorStatus attacker)
    {
        owner = attacker;
    }

    /// <summary>攻撃者・ダメージ計算・ダメージ量をまとめて設定する</summary>
    public void Setup(CharactorStatus attacker, DamageSource source, float value)
    {
        owner = attacker;
        damageSource = source;
        damageValue = value;
    }

    /// <summary>
    /// コードから生成した判定(Timeline の攻撃判定など)の設定をまとめて行う。
    /// 何回でも当たれる(maxHits 無制限)・自動では消えない設定になる。
    /// 消すときは呼び出し側が Destroy すること。
    /// </summary>
    public void Configure(CharactorStatus attacker, DamageSource source, float value,
                          IEnumerable<BuffGrant> grantBuffs, HitTargetType target,
                          LayerMask hurtMask, float rehit)
    {
        owner = attacker;
        damageSource = source;
        damageValue = value;
        SetBuffs(grantBuffs);
        targetType = target;
        hurtLayers = hurtMask;
        rehitInterval = rehit;

        maxHits = 0;                  // 同じ相手には rehitInterval ごと、全体では無制限
        destroyOnMaxHits = false;
        destroyOnObstacle = false;
        lifeTime = 0f;

        ResetHits();
    }

    /// <summary>付与するバフ・デバフを入れ替える</summary>
    public void SetBuffs(IEnumerable<BuffGrant> newBuffs)
    {
        // 自分自身のリストが渡されても壊れないよう、先にコピーする
        var copy = newBuffs != null ? new List<BuffGrant>(newBuffs) : new List<BuffGrant>();
        buffs.Clear();
        buffs.AddRange(copy);
    }

    /// <summary>判定を有効にする(近接攻撃の開始時に呼ぶ)。ヒット履歴もリセット</summary>
    public void Activate()
    {
        gameObject.SetActive(true);
        ResetHits();
    }

    /// <summary>判定を無効にする(近接攻撃の終了時に呼ぶ)</summary>
    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    /// <summary>ヒット履歴をリセットする(同じ相手にもう一度当たれるようになる)</summary>
    public void ResetHits()
    {
        hitTimes.Clear();
        hitCount = 0;
    }

    // ───────── 内部処理 ─────────

    /// <summary>触れたコライダーを調べて、必要ならヒット処理をする</summary>
    /// <param name="isEnter">触れた瞬間か(ログの出しすぎを防ぐため、理由ログは触れた瞬間だけ出す)</param>
    private void Process(Collider other, bool isEnter)
    {
        // コライダー自身か、その親にある HurtBox を探す(一番近いものを使う)
        HurtBox hurt = other.GetComponentInParent<HurtBox>();

        if (hurt == null)
        {
            if (isEnter)
                Log($"「{other.name}」に触れたが HurtBox が無い(コライダーのオブジェクトか、その親に HurtBox を付ける)");

            // 受ける判定ではない → 壁なら弾を消す
            if (destroyOnObstacle && IsInLayer(other.gameObject, obstacleLayers))
            {
                Destroy(gameObject);
            }
            return;
        }

        if (!IsInLayer(other.gameObject, hurtLayers))
        {
            if (isEnter)
                Log($"「{other.name}」はレイヤー「{LayerMask.LayerToName(other.gameObject.layer)}」で、Hurt Layers に含まれていない");
            return;
        }

        TryHit(hurt, isEnter);
    }

    /// <summary>条件を満たせばダメージとバフを与える</summary>
    private void TryHit(HurtBox hurt, bool isEnter)
    {
        CharactorStatus target = hurt.Status;

        if (target == null)
        {
            if (isEnter) Log($"HurtBox「{hurt.name}」に CharactorStatus が見つからない(親のどこかに付ける)");
            return;
        }

        if (target == owner) return;                        // 自分自身には当たらない

        if (!IsValidTarget(target))
        {
            if (isEnter)
                Log($"「{target.name}」は陣営の条件に合わない(Target Type={targetType}, " +
                    $"攻撃者 Is Enemy={(owner != null ? owner.IsEnemy.ToString() : "なし")}, 相手 Is Enemy={target.IsEnemy})");
            return;
        }

        if (maxHits > 0 && hitCount >= maxHits) return;     // もう当たりきった

        // 同じ相手への再ヒット制限
        float now = Time.time;
        if (hitTimes.TryGetValue(target, out float last))
        {
            if (rehitInterval <= 0f || now - last < rehitInterval) return;
        }
        hitTimes[target] = now;
        hitCount++;

        // ダメージとバフを与える(無敵なら Miss になり、バフも入らない)
        bool landed = hurt.ReceiveHit(CreateDamageInfo(), buffs, owner);
        Log($"ヒット:「{target.name}」 ダメージが通ったか={landed}(無敵だと false)");
        onHit?.Invoke(hurt);

        // 弾は最大ヒット回数に達したら消える
        if (destroyOnMaxHits && maxHits > 0 && hitCount >= maxHits)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>陣営の条件に合う相手か</summary>
    private bool IsValidTarget(CharactorStatus target)
    {
        if (targetType == HitTargetType.Everyone || owner == null) return true;

        bool sameTeam = target.IsEnemy == owner.IsEnemy;
        return targetType == HitTargetType.SameTeam ? sameTeam : !sameTeam;
    }

    /// <summary>ダメージ情報を作る(攻撃者の攻撃力・急所率を使う)</summary>
    private DamageInfo CreateDamageInfo()
    {
        if (damageSource == DamageSource.OwnerAttack && owner != null)
        {
            StatusBase s = owner.statusBase;
            return new DamageInfo
            {
                AttackPower = s.Attack * damageValue,
                IsCritical = Random.Range(0f, 100f) < s.CriticalRate,
                CriticalMultiplier = s.CriticalMult,
            };
        }

        // 固定ダメージ(攻撃者なしの場合もここ)
        return new DamageInfo
        {
            AttackPower = damageValue,
            IsCritical = false,
            CriticalMultiplier = 1f,
        };
    }

    /// <summary>オブジェクトが LayerMask に含まれるか</summary>
    private static bool IsInLayer(GameObject obj, LayerMask mask)
    {
        return (mask.value & (1 << obj.layer)) != 0;
    }

    /// <summary>デバッグログ(Debug Log がONのときだけ出る)</summary>
    private void Log(string message)
    {
        if (debugLog) Debug.Log($"[HitBox:{name}] {message}", this);
    }
}
