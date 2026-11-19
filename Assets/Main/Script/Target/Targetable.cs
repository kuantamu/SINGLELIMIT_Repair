
using UnityEngine;

/// <summary>
/// ターゲット対象として必要な情報を提供する。
/// </summary>
[DisallowMultipleComponent]
public class Targetable : MonoBehaviour
{
    [Header("キャラクターステータス")]
    [SerializeField]
    private CharactorStatus status;

    [Header("ターゲット位置")]
    [SerializeField]
    private Transform targetPoint;

    public CharactorStatus Status => status;

    public Transform TargetPoint =>
        targetPoint != null ? targetPoint : transform;

    public bool IsAlive =>
        status != null &&
        status.statusBase.HitState != HitState.Dead &&
        status.statusBase.CurrentHP > 0;

    public Faction IsEnemy =>
        status.statusBase.Faction;

    private void Awake()
    {
        if (status == null)
            status = GetComponentInParent<CharactorStatus>();
    }
}
