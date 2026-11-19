
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TimelineActionPlayer actionPlayer;
    [SerializeField] private TargetingSystem targetingSystem;

    [Header("Timeline Settings")]
    [SerializeField] private string timelineGroup = "Enemy_1";

    [Header("Action IDs")]
    [SerializeField] private int idleActionId = 0;
    [SerializeField] private int strafeActionId = 1;
    [SerializeField] private int attackActionId = 2;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float strafeSpeed = 1f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float preferredDistance = 3f;
    [SerializeField] private float attackCooldownDuration = 1.5f;

    [Header("Action Random Setting")]
    [SerializeField] private float RandomAttackIntervalMin = 2f;
    [SerializeField] private float RandomAttackIntervalMax = 10f;



    private Rigidbody rb;

    private EnemyMovement movement;
    private EnemySensor sensor;
    private EnemyStateMachine stateMachine;

    private string expectedPresetName;
    private int expectedActionId = -1;

    public float AttackCooldownDuration =>
        attackCooldownDuration;

    public float AtkRandMin => RandomAttackIntervalMin;
    public float AtkRandMax => RandomAttackIntervalMax;

    public event Action AttackAnimationEnded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (actionPlayer == null)
            actionPlayer = GetComponent<TimelineActionPlayer>();

        movement = new EnemyMovement(
            rb,
            transform,
            moveSpeed,
            strafeSpeed,
            rotationSpeed
        );

        if (targetingSystem == null)
        {
            targetingSystem = GetComponentInChildren<TargetingSystem>();
        }

        sensor = new EnemySensor(targetingSystem);

        stateMachine = new EnemyStateMachine(
            this,
            movement,
            sensor,
            attackRange,
            preferredDistance
        );
    }


    private void OnEnable()
    {
        if (actionPlayer != null)
            actionPlayer.PlaybackEnded += OnPlaybackEnded;

        stateMachine?.Enable();
    }

    private void OnDisable()
    {
        if (actionPlayer != null)
            actionPlayer.PlaybackEnded -= OnPlaybackEnded;

        stateMachine?.Disable();
    }

    private void Start()
    {
        stateMachine.Initialize();
    }

    private void FixedUpdate()
    {
        // 状態更新は物理更新に合わせる
        stateMachine?.Tick(Time.fixedDeltaTime);
    }

    public void PlayIdle()
    {
        PlayLoopAction(idleActionId);
    }

    public void PlayStrafe()
    {
        PlayLoopAction(strafeActionId);
    }

    public void PlayAttack()
    {
        if (actionPlayer == null)
            return;

        expectedPresetName = timelineGroup;
        expectedActionId = attackActionId;

        actionPlayer.ForcePlay(
            timelineGroup,
            attackActionId,
            -1f,
            -1f,
            false
        );
    }

    public void CancelAttackAndPlayStrafe()
    {
        expectedActionId = -1;

        if (actionPlayer == null)
            return;

        actionPlayer.ForcePlay(
            timelineGroup,
            strafeActionId
        );
    }

    private void PlayLoopAction(int actionId)
    {
        if (actionPlayer == null)
            return;

        expectedActionId = -1;

        actionPlayer.Play(
            timelineGroup,
            actionId
        );
    }
    // 状態遷移時のアニメーション切り替え
    private void PlayAction(int actionId)
    {
        if (actionPlayer == null)
            return;

        expectedActionId = -1;

        actionPlayer.ForcePlay(
            timelineGroup,
            actionId
        );
    }

    private void OnPlaybackEnded(
        string presetName,
        TimelineActionPreset.Entry entry)
    {
        if (presetName != expectedPresetName)
            return;

        if (entry.id != expectedActionId)
            return;

        expectedActionId = -1;
        AttackAnimationEnded?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);
    }
}
