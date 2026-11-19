
public class ExploreState : IEnemyState
{
    private readonly EnemyStateMachine machine;

    public ExploreState(EnemyStateMachine machine)
    {
        this.machine = machine;
    }

    public void Enter()
    {
        machine.ClearTarget();
        machine.Stop();
        machine.PlayIdle();
    }

    public void Update()
    {
        if (!machine.HasTarget)
            return;

        machine.ChangeState(machine.StrafeState);
    }

    public void Exit()
    {
    }
}
