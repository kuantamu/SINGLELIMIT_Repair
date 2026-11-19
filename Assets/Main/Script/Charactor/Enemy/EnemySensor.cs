
using UnityEngine;

public class EnemySensor
{
    private readonly TargetingSystem targetingSystem;

    public EnemySensor(TargetingSystem targetingSystem)
    {
        this.targetingSystem = targetingSystem;
    }

    // 現在の対象
    public Targetable CurrentTarget
    {
        get
        {
            if (targetingSystem == null)
                return null;

            return targetingSystem.CurrentTarget;
        }
    }

    // 対象が存在するか
    public bool HasTarget()
    {
        return CurrentTarget != null;
    }

    // 対象のTransform
    public Transform TargetTransform
    {
        get
        {
            Targetable target = CurrentTarget;

            return target != null
                ? target.transform
                : null;
        }
    }

    // 対象の位置
    public Vector3 TargetPosition
    {
        get
        {
            Targetable target = CurrentTarget;

            return target != null
                ? target.TargetPoint.position
                : Vector3.zero;
        }
    }

    // 対象を解除
    public void ClearTarget()
    {
        targetingSystem?.ClearTarget();
    }
}
