
using UnityEngine;

public class AttackState : IEnemyState
{
    private readonly EnemyStateMachine machine;

    private bool animationEnded;

    public AttackState(EnemyStateMachine machine)
    {
        this.machine = machine;
    }

    public void Enter()
    {
        animationEnded = false;

        machine.Stop();
        machine.PlayAttack();
    }

    public void Update()
    {
        if (!machine.HasTarget)
        {
            machine.ChangeState(machine.ExploreState);
            return;
        }

        if (animationEnded)
        {
            machine.StartAttackCooldown();
            machine.ChangeState(machine.StrafeState);
        }
    }

    public void OnAnimationEnded()
    {
        animationEnded = true;
    }

    public void Exit()
    {
        machine.Stop();
        machine.CancelAttackAnimation();
    }
}
