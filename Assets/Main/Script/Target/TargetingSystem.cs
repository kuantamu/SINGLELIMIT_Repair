
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public class TargetingSystem : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private Targetable owner;

    [SerializeField]
    private Transform viewTransform;

    [Header("検索設定")]
    [SerializeField, Min(0.1f)]
    private float searchRadius = 15f;

    [SerializeField]
    private Faction factionFilter = Faction.Enemy;

    [Header("視野判定")]
    [SerializeField]
    private bool useViewCheck = false;

    [SerializeField, Range(0f, 360f)]
    private float viewAngle = 90f;

    [SerializeField]
    private LayerMask targetLayer = ~9;

    [Header("遮蔽判定")]
    [SerializeField]
    private bool useLineOfSight = false;

    [SerializeField]
    private LayerMask obstacleMask;

    [Header("スコア設定")]
    [SerializeField, Min(0f)]
    private float distanceWeight = 0.7f;

    [SerializeField, Min(0f)]
    private float angleWeight = 0.3f;

    [Header("ターゲット維持")]
    [SerializeField]
    private bool autoSelect = true;

    [SerializeField, Range(0f, 1f)]
    private float switchScoreAdvantage = 0.1f;

    private SphereCollider sensorCollider;

    // 候補と接触中のColliderを管理
    private readonly Dictionary<Targetable, HashSet<Collider>>
        candidates = new();

    private readonly List<Targetable> removeTargets = new();

    private readonly List<Targetable> switchCandidates = new();

    public Targetable CurrentTarget { get; private set; }

    public IReadOnlyCollection<Targetable> Candidates =>
        candidates.Keys;

    public bool IsManualTarget { get; private set; }

    private void Awake()
    {
        sensorCollider = GetComponent<SphereCollider>();

        if (owner == null)
            owner = GetComponent<Targetable>();

        if (viewTransform == null)
            viewTransform = transform;

        sensorCollider.isTrigger = true;
        UpdateSensorRadius();
    }

    private void OnEnable()
    {
        RebuildCandidates();
    }

    private void Update()
    {
        // 手動ターゲットが無効になった場合
        if (IsManualTarget &&
            !IsValidTarget(CurrentTarget))
        {
            ClearTarget();
        }

        if (autoSelect && !IsManualTarget)
            RefreshTarget();
    }

    private void OnValidate()
    {
        if (sensorCollider == null)
            sensorCollider = GetComponent<SphereCollider>();

        UpdateSensorRadius();
    }

    private void UpdateSensorRadius()
    {
        if (sensorCollider == null)
            return;

        sensorCollider.isTrigger = true;
        sensorCollider.radius = searchRadius;
    }

    // =================================
    // 候補の検出・管理
    // =================================

    private void OnTriggerEnter(Collider other)
    {
        RegisterCandidate(other);
    }

    private void OnTriggerExit(Collider other)
    {
        UnregisterCandidate(other);
    }

    private void RegisterCandidate(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetLayer.value) == 0)
            return;

        Targetable target =
            other.GetComponentInParent<Targetable>();

        if (target == null || target == owner)
            return;

        if (!candidates.TryGetValue(target, out var colliders))
        {
            colliders = new HashSet<Collider>();
            candidates.Add(target, colliders);
        }

        colliders.Add(other);
    }

    private void UnregisterCandidate(Collider other)
    {
        Targetable target =
            other.GetComponentInParent<Targetable>();

        if (target == null)
            return;

        if (!candidates.TryGetValue(target, out var colliders))
            return;

        colliders.Remove(other);

        if (colliders.Count == 0)
        {
            candidates.Remove(target);

            if (CurrentTarget == target)
                ClearTarget();
        }
    }

    private void RebuildCandidates()
    {
        if (sensorCollider == null)
            return;

        candidates.Clear();

        Vector3 center =
            sensorCollider.transform.TransformPoint(
                sensorCollider.center
            );

        Vector3 scale = sensorCollider.transform.lossyScale;

        float maxScale = Mathf.Max(
            Mathf.Abs(scale.x),
            Mathf.Abs(scale.y),
            Mathf.Abs(scale.z)
        );

        float radius = sensorCollider.radius * maxScale;

        Collider[] colliders = Physics.OverlapSphere(
            center,
            radius,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider col in colliders)
        {
            if (col == sensorCollider)
                continue;

            RegisterCandidate(col);
        }
    }

    // =================================
    // 候補の整理
    // =================================

    private void PruneCandidates()
    {
        removeTargets.Clear();

        foreach (var pair in candidates)
        {
            Targetable target = pair.Key;

            if (target == null)
            {
                removeTargets.Add(target);
                continue;
            }

            pair.Value.RemoveWhere(col => col == null);

            if (pair.Value.Count == 0)
                removeTargets.Add(target);
        }

        foreach (Targetable target in removeTargets)
            candidates.Remove(target);
    }

    // =================================
    // ターゲット自動選択
    // =================================

    public void RefreshTarget()
    {
        if (owner == null || !owner.IsAlive)
        {
            ClearTarget();
            return;
        }

        PruneCandidates();

        // 手動選択中は自動で変更しない
        if (IsManualTarget && IsValidTarget(CurrentTarget))
            return;

        IsManualTarget = false;

        Targetable bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (Targetable target in candidates.Keys)
        {
            if (!IsValidTarget(target))
                continue;

            float score = CalculateScore(target);

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        // 現在のターゲットが有効なら維持を優先
        if (IsValidTarget(CurrentTarget))
        {
            float currentScore = CalculateScore(CurrentTarget);

            if (bestTarget == null ||
                bestTarget == CurrentTarget)
            {
                return;
            }

            // 新しい候補が十分に優れている場合だけ切り替える
            if (bestScore + switchScoreAdvantage >= currentScore)
                return;
        }

        CurrentTarget = bestTarget;
    }

    // =================================
    // ターゲット有効判定
    // =================================

    private bool IsValidTarget(Targetable target)
    {
        if (target == null || target == owner)
            return false;

        if (!candidates.ContainsKey(target))
            return false;

        if (!target.IsAlive)
            return false;

        if (target.Status == null)
            return false;

        // 陣営判定
        if (target.Status.statusBase.Faction != factionFilter)
            return false;

        Vector3 direction =
            target.TargetPoint.position - viewTransform.position;

        float distance = direction.magnitude;

        if (distance <= 0f)
            return false;

        // 視野角判定
        if (useViewCheck)
        {
            float angle = Vector3.Angle(
                viewTransform.forward,
                direction
            );

            if (angle > viewAngle * 0.5f)
                return false;
        }

        // 遮蔽判定
        if (useLineOfSight &&
            !HasLineOfSight(target, direction, distance))
        {
            return false;
        }

        return true;
    }

    // =================================
    // スコア計算
    // =================================

    private float CalculateScore(Targetable target)
    {
        Vector3 direction =
            target.TargetPoint.position - viewTransform.position;

        float distance = direction.magnitude;

        float normalizedDistance =
            Mathf.Clamp01(distance / searchRadius);

        float angle = Vector3.Angle(
            viewTransform.forward,
            direction
        );

        float normalizedAngle = angle / 180f;

        float totalWeight = distanceWeight + angleWeight;

        if (totalWeight <= 0f)
            return normalizedDistance;

        return (
            normalizedDistance * distanceWeight +
            normalizedAngle * angleWeight
        ) / totalWeight;
    }

    // =================================
    // 遮蔽判定
    // =================================

    private bool HasLineOfSight(
        Targetable target,
        Vector3 direction,
        float distance)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            viewTransform.position,
            direction.normalized,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.transform;

            // 自分自身を無視
            if (hitTransform == transform ||
                hitTransform.IsChildOf(transform))
            {
                continue;
            }

            // ターゲット自身を無視
            if (hitTransform == target.transform ||
                hitTransform.IsChildOf(target.transform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    // =================================
    // 手動ターゲット切り替え
    // =================================

    /// <summary>
    /// 手動でターゲットを指定する。
    /// 指定後は自動選択で上書きされない。
    /// </summary>
    public bool SetTarget(Targetable target)
    {
        if (!IsValidTarget(target))
            return false;

        CurrentTarget = target;
        IsManualTarget = true;

        return true;
    }

    /// <summary>
    /// ターゲットを次または前の候補に切り替える。
    /// direction: 1 = 次、-1 = 前
    /// </summary>
    public bool SwitchTarget(int direction)
    {
        if (direction == 0)
            return false;

        PruneCandidates();

        switchCandidates.Clear();

        foreach (Targetable target in candidates.Keys)
        {
            if (IsValidTarget(target))
                switchCandidates.Add(target);
        }

        if (switchCandidates.Count <= 1)
            return false;

        // スコア順に並べる
        switchCandidates.Sort(
            (a, b) =>
                CalculateScore(a).CompareTo(CalculateScore(b))
        );

        int currentIndex =
            switchCandidates.IndexOf(CurrentTarget);

        int nextIndex;

        if (currentIndex < 0)
        {
            nextIndex = direction > 0
                ? 0
                : switchCandidates.Count - 1;
        }
        else
        {
            int step = direction > 0 ? 1 : -1;

            nextIndex =
                (currentIndex + step + switchCandidates.Count)
                % switchCandidates.Count;
        }

        return SetTarget(switchCandidates[nextIndex]);
    }

    /// <summary>
    /// 次のターゲットへ切り替える
    /// </summary>
    public bool SelectNextTarget()
    {
        Debug.Log($"候補数: {candidates.Count}");
        return SwitchTarget(1);
    }

    /// <summary>
    /// 前のターゲットへ切り替える
    /// </summary>
    public bool SelectPreviousTarget()
    {
        return SwitchTarget(-1);
    }

    // =================================
    // ターゲット解除・自動選択復帰
    // =================================

    /// <summary>
    /// ターゲットを解除し、自動選択を再開可能にする
    /// </summary>
    public void ClearTarget()
    {
        CurrentTarget = null;
        IsManualTarget = false;
    }

    /// <summary>
    /// 手動ロックを解除する。
    /// 現在のターゲットは維持し、自動選択に戻す。
    /// </summary>
    public void ResumeAutoTargeting()
    {
        IsManualTarget = false;

        if (autoSelect)
            RefreshTarget();
    }

    private void OnDisable()
    {
        candidates.Clear();
        switchCandidates.Clear();

        CurrentTarget = null;
        IsManualTarget = false;
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform origin = viewTransform != null
            ? viewTransform
            : transform;

        Vector3 position = origin.position;
        Vector3 forward = origin.forward;

        // 探索範囲
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, searchRadius);

        // 視界判定が無効なら、視野角は描画しない
        if (!useViewCheck)
            return;

        // 視野角の描画
        Gizmos.color = Color.green;

        float halfAngle = viewAngle * 0.5f;
        int segments = 32;

        Vector3 previousPoint = Vector3.zero;

        // 視野角の左右端
        Vector3 leftDirection =
            Quaternion.AngleAxis(-halfAngle, origin.up) * forward;

        Vector3 rightDirection =
            Quaternion.AngleAxis(halfAngle, origin.up) * forward;

        Gizmos.DrawLine(
            position,
            position + leftDirection * searchRadius
        );

        Gizmos.DrawLine(
            position,
            position + rightDirection * searchRadius
        );

        // 視野角の円弧
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Lerp(
                -halfAngle,
                halfAngle,
                (float)i / segments
            );

            Vector3 direction =
                Quaternion.AngleAxis(angle, origin.up) * forward;

            Vector3 point =
                position + direction * searchRadius;

            if (i > 0)
                Gizmos.DrawLine(previousPoint, point);

            previousPoint = point;
        }

        // 正面方向
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(position, forward * searchRadius);
    }
#endif

}
