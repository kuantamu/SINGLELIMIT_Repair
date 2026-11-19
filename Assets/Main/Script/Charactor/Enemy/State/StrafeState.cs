
using UnityEngine;

public class StrafeState : IEnemyState
{
    private readonly EnemyStateMachine machine;

    private float directionChangeTimer;
    private int strafeDirection = 1;

    private const float DirectionChangeInterval = 3f;
    private const float DistanceTolerance = 0.5f;

    private enum BehaviorPhase
    {
        Approach,   // 接近
        Retreat,    // 後退
        Observe,   // 様子見
        Lateral    // 横移動
    }
    BehaviorPhase currentPhase = BehaviorPhase.Observe;

    public StrafeState(EnemyStateMachine machine)
    {
        this.machine = machine;
    }

    public void Enter()
    {
        directionChangeTimer = 0f;
        machine.PlayStrafe();
    }

    public void Update()
    {
        // 対象が存在しない場合は探索状態に遷移
        if (!machine.HasTarget)
        {
            machine.ChangeState(machine.ExploreState);
            return;
        }

        Vector3 toTarget =
            machine.TargetPosition - machine.EnemyPosition;

        toTarget.y = 0f;

        float distance = toTarget.magnitude;



        Vector3 direction = toTarget.normalized;

        if (distance <= machine.AttackRange)
        {
            if (machine.AttackCooldown <= 0f)
            {
                machine.ChangeState(machine.AttackState);
                return;
            }
            else
            {
                currentPhase = BehaviorPhase.Observe;
            }
        }
        else
        {
            currentPhase = BehaviorPhase.Approach;
        }


        //if (machine.AttackCooldown <= 0f)
        //{
        //    if (distance <= machine.AttackRange)
        //    {
        //        machine.ChangeState(machine.AttackState);
        //        return;
        //    }
        //    else
        //    {
        //        currentPhase = BehaviorPhase.Approach;
        //        StatePlay(direction);
        //        return;
        //    }
        //}

        //// プレイヤーとの距離が好ましい距離よりも大きい場合は前進
        //if (distance > machine.PreferredDistance + 0.5f)
        //{
        //    currentPhase = BehaviorPhase.Approach;
        //}
        //// プレイヤーとの距離が好ましい距離よりも小さい場合は後退
        //else if (distance < machine.PreferredDistance - 0.5f)
        //{
        //    currentPhase = BehaviorPhase.Retreat;
        //}
        //// プレイヤーとの距離が好ましい距離の範囲内の場合は横移動
        //else
        //{
        //    if (currentPhase != BehaviorPhase.Lateral && currentPhase != BehaviorPhase.Observe)
        //    {
        //        currentPhase = Random.Range(0, 2) == 0 ? BehaviorPhase.Lateral : BehaviorPhase.Observe;
        //    }
        //}
        StatePlay(direction);
    }


    void StatePlay(Vector3 direction)
    {
        switch (currentPhase)
        {
            case BehaviorPhase.Approach:
                // Implement approach behavior
                machine.Move(direction);
                break;
            case BehaviorPhase.Retreat:
                // Implement retreat behavior
                machine.StrafeMove(-direction);
                break;
            case BehaviorPhase.Observe:
                // Implement observe behavior
                machine.PlayIdle();
                UpdateObserveDirection();
                Debug.Log("Observing...");
                break;
            case BehaviorPhase.Lateral:
                // Implement lateral movement behavior
                machine.PlayStrafe();
                UpdateStrafeDirection();

                Vector3 lateralDirection =
                    Vector3.Cross(Vector3.up, direction)
                    * strafeDirection;
                machine.StrafeMove(lateralDirection);
                Debug.Log("Strafing...");
                break;
        }
    }
    private Vector3 GetEnemyPosition()
    {
        return machine.EnemyPosition;
    }

    
    private void UpdateStrafeDirection()
    {
        directionChangeTimer += Time.deltaTime;

        if (directionChangeTimer < DirectionChangeInterval)
            return;

        directionChangeTimer = 0f;
        strafeDirection *= -1;
        currentPhase = BehaviorPhase.Observe;
    }
    private void UpdateObserveDirection()
    {
        directionChangeTimer += Time.deltaTime;

        if (directionChangeTimer < 2)
            return;

        directionChangeTimer = 0f;
        currentPhase = BehaviorPhase.Lateral;
    }
    public void Exit()
    {
        machine.Stop();
    }
}
