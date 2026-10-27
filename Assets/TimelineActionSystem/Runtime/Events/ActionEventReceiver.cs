using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ActionEventReceiver : MonoBehaviour
{
    [SerializeField] private Transform effectRoot;
    [SerializeField] private AudioSource audioSource;

    [Header("攻撃判定")]
    [Tooltip("攻撃判定 Box の基準になる Transform。未設定なら Effect Root、それも無ければこのオブジェクト")]
    [SerializeField] private Transform attackRoot;

    [Tooltip("このレシーバーのキャラ(攻撃力・陣営の判定、自分へのバフ付与の対象)。未設定ならこのオブジェクトか親から探す")]
    [SerializeField] private CharactorStatus attacker;

    [Tooltip("ONにすると、攻撃判定の生成状況と当たらなかった理由をConsoleに出す")]
    [SerializeField] private bool debugLog = false;

    // 攻撃IDごとの有効数(同じIDが重なっても、全部終わるまで有効扱い)
    private readonly Dictionary<string, int> attackCounts = new();
    private readonly HashSet<GameObject> activeVfx = new();

    // 今有効な攻撃判定 Box
    private readonly List<AttackBox> activeBoxes = new();

    // エディットモードで表示する Box の色(再生中は AttackHitboxDebugView の色)
    private static readonly Color EditFill = new Color(1f, 0.15f, 0.15f, 0.25f);
    private static readonly Color EditLine = new Color(1f, 0.2f, 0.2f, 1f);

    /// <summary>
    /// 1つの攻撃判定(Box)。Timeline のクリップ1つにつき1つ生成される。
    /// エディットモードでは Instance は null で、表示用のデータだけを持つ。
    /// </summary>
    public sealed class AttackBox
    {
        public string Id { get; }
        public AttackHitSettings Settings { get; }
        public Transform Root { get; }

        /// <summary>実際に生成した判定オブジェクト(再生中のみ)</summary>
        public GameObject Instance { get; set; }

        public AttackBox(string id, AttackHitSettings settings, Transform root)
        {
            Id = id;
            Settings = settings;
            Root = root;
        }

        /// <summary>Box の大きさ(ローカル。0にならないよう下限あり)</summary>
        public Vector3 Size => new Vector3(
            Mathf.Max(0.001f, Mathf.Abs(Settings.size.x)),
            Mathf.Max(0.001f, Mathf.Abs(Settings.size.y)),
            Mathf.Max(0.001f, Mathf.Abs(Settings.size.z)));

        /// <summary>Box 中心のローカル→ワールド変換(基準のスケールも含む)</summary>
        public Matrix4x4 LocalToWorld =>
            Root.localToWorldMatrix *
            Matrix4x4.TRS(
                Settings.positionOffset,
                Quaternion.Euler(Settings.rotationOffset),
                Vector3.one);
    }

    /// <summary>今有効な攻撃判定の一覧(デバッグ表示用・読み取り専用)</summary>
    public IReadOnlyList<AttackBox> ActiveAttackBoxes => activeBoxes;

    private void Awake()
    {
        if (attacker == null)
            attacker = GetComponentInParent<CharactorStatus>();

        if (attacker == null)
        {
            Debug.LogWarning(
                "CharactorStatus が見つかりません。攻撃力・陣営が使えず、" +
                "ダメージは固定値(Damage Value)になります。Attacker を設定してください。", this);
        }
    }

    private void OnDisable()
    {
        StopAllAttackBoxes();
    }

    // エディットモード(Timeline プレビュー中)に Box を Scene ビューで見せる。
    // 再生中は AttackHitboxDebugView が担当する。
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
            return;

        AttackHitboxDebugView.DrawBoxes(activeBoxes, EditFill, EditLine);
    }

    public GameObject PlayVfx(
        GameObject prefab,
        float duration,
        Vector3 positionOffset,
        Vector3 rotationOffset,
        Vector3 scaleMultiplier)
    {
        if (prefab == null)
            return null;

        Transform root = effectRoot != null
            ? effectRoot
            : transform;

        Vector3 position = root.TransformPoint(positionOffset);

        Quaternion rotation =
            root.rotation * Quaternion.Euler(rotationOffset);


        GameObject instance = Instantiate(
            prefab, position, rotation, root);

        instance.transform.localScale = Vector3.Scale(
            instance.transform.localScale,
            scaleMultiplier);

        activeVfx.Add(instance);

        if (Application.isPlaying)
        {
            StartCoroutine(
                DestroyVfxAfter(instance, Mathf.Max(0.01f, duration)));
        }
        else
        {
            // エディットモードのプレビュー専用オブジェクト
            instance.hideFlags = HideFlags.DontSave;
        }

        return instance;

    }

    private IEnumerator DestroyVfxAfter(
        GameObject instance,
        float duration)
    {
        yield return new WaitForSeconds(duration);

        if (instance != null)
            StopVfx(instance);
    }


    public void StopVfx(GameObject instance)
    {
        if (instance == null)
            return;

        activeVfx.Remove(instance);

        if (Application.isPlaying)
            Destroy(instance);
        else
            DestroyImmediate(instance);
    }

    public void StopAllVfx()
    {
        foreach (GameObject instance in activeVfx)
        {
            if (instance == null)
                continue;

            if (Application.isPlaying)
                Destroy(instance);
            else
                DestroyImmediate(instance);
        }

        activeVfx.Clear();
    }


    public void PlaySfx(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    // --------------------------------------------------
    // 攻撃判定
    // --------------------------------------------------

    /// <summary>
    /// 攻撃判定(Box)を生成して判定を開始する。クリップの開始時に呼ばれる。
    /// 再生中は実際に当たり判定を生成し、エディットモードは表示用のデータだけ持つ。
    /// </summary>
    /// <returns>生成した判定(EndAttack に渡して消す)</returns>
    public AttackBox BeginAttack(string attackId, AttackHitSettings settings)
    {
        settings ??= new AttackHitSettings();

        Transform root = attackRoot != null
            ? attackRoot
            : (effectRoot != null ? effectRoot : transform);

        var box = new AttackBox(attackId, settings, root);
        activeBoxes.Add(box);

        if (debugLog)
        {
            Debug.Log(
                $"[ActionEventReceiver] 攻撃判定を開始: id=「{attackId}」 基準={root.name} " +
                $"再生中={Application.isPlaying} 攻撃者={(attacker != null ? attacker.name : "なし")} " +
                $"位置={box.LocalToWorld.GetPosition()} 大きさ={box.Size}", this);
        }

        // 攻撃IDの有効管理(従来どおり)
        SetAttackWindow(attackId, true);

        if (Application.isPlaying)
            SpawnHitBox(box);

        RepaintScene();
        return box;
    }

    /// <summary>攻撃判定を消して判定を終了する。クリップの終了時に呼ばれる</summary>
    public void EndAttack(AttackBox box)
    {
        if (box == null)
            return;

        activeBoxes.Remove(box);
        DestroyHitBox(box);

        SetAttackWindow(box.Id, false);
        RepaintScene();
    }

    /// <summary>有効な攻撃判定をすべて消す</summary>
    public void StopAllAttackBoxes()
    {
        foreach (AttackBox box in activeBoxes)
            DestroyHitBox(box);

        activeBoxes.Clear();
        attackCounts.Clear();
        RepaintScene();
    }

    /// <summary>指定した攻撃IDが今有効か</summary>
    public bool IsAttackActive(string attackId)
    {
        return !string.IsNullOrWhiteSpace(attackId) &&
               attackCounts.ContainsKey(attackId);
    }

    public void SetAttackWindow(string attackId, bool active)
    {
        if (string.IsNullOrWhiteSpace(attackId))
            return;

        attackCounts.TryGetValue(attackId, out int before);

        int after = active
            ? before + 1
            : Mathf.Max(0, before - 1);

        if (after == 0)
            attackCounts.Remove(attackId);
        else
            attackCounts[attackId] = after;

        // 有効↔無効が切り替わったときだけ通知する
        bool changed = (before == 0) != (after == 0);

        if (!changed)
            return;

        Debug.Log(
            $"Attack: {attackId}, Active: {active}", this);
    }

    /// <summary>Box の位置・向き・大きさで、実際に当たり判定のオブジェクトを生成する</summary>
    private void SpawnHitBox(AttackBox box)
    {
        AttackHitSettings s = box.Settings;

        var go = new GameObject($"AttackBox_{box.Id}");
        go.transform.SetParent(box.Root, false);
        go.transform.localPosition = s.positionOffset;
        go.transform.localRotation = Quaternion.Euler(s.rotationOffset);
        go.layer = box.Root.gameObject.layer;

        // HitBox は Collider が必要なので、先に Box を付ける
        var col = go.AddComponent<BoxCollider>();
        col.size = box.Size;
        col.isTrigger = true;

        var hit = go.AddComponent<HitBox>();
        hit.Configure(
            attacker,
            s.damageSource,
            s.damageValue,
            s.buffs,
            s.targetType,
            s.hurtLayers,
            s.rehitInterval);

        hit.DebugLog = debugLog;
        box.Instance = go;

        if (debugLog)
            Debug.Log($"[ActionEventReceiver] 判定オブジェクト「{go.name}」を生成(レイヤー={LayerMask.LayerToName(go.layer)})", go);
    }

    private void DestroyHitBox(AttackBox box)
    {
        if (box.Instance == null)
            return;

        if (Application.isPlaying)
            Destroy(box.Instance);
        else
            DestroyImmediate(box.Instance);

        box.Instance = null;
    }

    private static void RepaintScene()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.SceneView.RepaintAll();
#endif
    }

    // --------------------------------------------------
    // 自分へのバフ・デバフ
    // --------------------------------------------------

    /// <summary>
    /// 自分(Attacker に設定したキャラ)にバフ・デバフを付与する。
    /// 再生中だけ働く(エディットモードの Timeline プレビューでは、ステータスを変えない)。
    /// </summary>
    public void ApplyBuffsToSelf(IEnumerable<BuffGrant> grants)
    {
        // プレビュー中に付与すると、シーン上のステータスが書き換わってしまうため
        if (!Application.isPlaying || grants == null)
            return;

        if (attacker == null)
        {
            Debug.LogWarning(
                "自分への付与に失敗: CharactorStatus が見つかりません。Attacker を設定してください。", this);
            return;
        }

        if (debugLog)
            Debug.Log($"[ActionEventReceiver] 自分「{attacker.name}」にバフ・デバフを付与", this);

        attacker.GiveBuffs(attacker, grants);
    }

    public void ResetAllAttackWindows()
    {
        StopAllAttackBoxes();
        StopAllVfx();

        SetComboWindow(false);
    }

    public event Action<bool> ComboWindowChanged;

    public void SetComboWindow(bool open)
    {
        ComboWindowChanged?.Invoke(open);
    }
}
