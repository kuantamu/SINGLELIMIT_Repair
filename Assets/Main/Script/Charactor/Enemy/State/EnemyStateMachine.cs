using System.Data.SqlTypes;
using UnityEngine;

public interface IEnemyState
{
    void Enter();
    void Update();
    void Exit();
}

public class EnemyStateMachine : MonoBehaviour
{
    private readonly EnemyController controller;
    private readonly EnemyMovement movement;
    private readonly EnemySensor sensor;

    private readonly ExploreState exploreState;
    private readonly StrafeState strafeState;
    private readonly AttackState attackState;

    private IEnemyState currentState;

    public float AttackCooldown { get; private set; }

    public float AttackRange { get; }
    public float PreferredDistance { get; }

    public Vector3 EnemyPosition => controller.transform.position;

    // 現在の対象
    public Targetable CurrentTarget => sensor.CurrentTarget;

    // 対象の位置
    public Vector3 TargetPosition => sensor.TargetPosition;

    // 対象が存在するか
    public bool HasTarget => sensor.HasTarget();

    public float AttackRandamMin => controller.AtkRandMin;
    public float AttackRandamMax => controller.AtkRandMax;

    public EnemyStateMachine(
        EnemyController controller,
        EnemyMovement movement,
        EnemySensor sensor,
        float attackRange,
        float preferredDistance)
    {
        this.controller = controller;
        this.movement = movement;
        this.sensor = sensor;

        AttackRange = attackRange;
        PreferredDistance = preferredDistance;

        exploreState = new ExploreState(this);
        strafeState = new StrafeState(this);
        attackState = new AttackState(this);
    }

    // イベント登録
    public void Enable()
    {
        controller.AttackAnimationEnded += OnAttackAnimationEnded;
    }

    // イベント解除
    public void Disable()
    {
        controller.AttackAnimationEnded -= OnAttackAnimationEnded;
    }

    public void Initialize()
    {
        ChangeState(exploreState);
    }

    public void Tick(float deltaTime)
    {
        if (AttackCooldown > 0f)
            AttackCooldown = Mathf.Max(0f, AttackCooldown - deltaTime);

        // ターゲットがいる間は相手の方向を向く
        if (HasTarget)
            movement.LookAt(TargetPosition, deltaTime);

        currentState?.Update();
    }

    public void ChangeState(IEnemyState nextState)
    {
        if (currentState == nextState)
            return;

        currentState?.Exit();

        currentState = nextState;

        currentState?.Enter();
    }

    public void ClearTarget()
    {
        sensor.ClearTarget();
    }

    public void Move(Vector3 direction)
    {
        movement.Move(direction);
    }
    public void StrafeMove(Vector3 direction)
    {
        movement.StrafeMove(direction);
    }


    public void Stop()
    {
        movement.Stop();
    }

    public void PlayIdle()
    {
        controller.PlayIdle();
    }

    public void PlayStrafe()
    {
        controller.PlayStrafe();
    }

    public void PlayAttack()
    {
        controller.PlayAttack();
    }

    public void StartAttackCooldown()
    {
        AttackCooldown = Random.Range(AttackRandamMin, AttackRandamMax);
        Debug.Log($"AttackCooldown set to {AttackCooldown} seconds.");
    }

    public void OnAttackAnimationEnded()
    {
        attackState.OnAnimationEnded();
    }
    public void CancelAttackAnimation()
    {
        controller.CancelAttackAndPlayStrafe();
    }

    public ExploreState ExploreState => exploreState;
    public StrafeState StrafeState => strafeState;
    public AttackState AttackState => attackState;
}
