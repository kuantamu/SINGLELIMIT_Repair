using UnityEngine;

public enum PlayerActionState
{
    Locomotion, // 通常移動
    Guard,      // ガード
    Dodge,      // 回避
    Attack,     // 攻撃
    Charge,     // チャージ
    Airborne    // 空中
}

[RequireComponent(typeof(PlayerMovement))]
public class PlayerActionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TimelineActionPlayer actionPlayer;

    private PlayerMovement playerMove;
    private CharaGroundColDitection charaGround;

    [Header("Timeline Action IDs")]
    [SerializeField] private string timelineGroup = "Player_1";
    [SerializeField] private int idleActionId = 0;
    [SerializeField] private int moveActionId = 1;
    [SerializeField] private int guardActionId = 3;
    [SerializeField] private int dodgeActionId = 4;
    [SerializeField] private int[] attackActionIds = { 2, 5, 6 };

    public PlayerActionState CurrentState { get; private set; }
        = PlayerActionState.Locomotion;

    private int attackStep;
    private bool comboWindowOpen;
    private bool dodgeCancelOpen;

    private void Awake()
    {
        playerMove = GetComponent<PlayerMovement>();
        charaGround = GetComponentInChildren<CharaGroundColDitection>();
    }

    public void HandleInput(IInputProvider input)
    {
        bool grounded = charaGround.GetGround();

        // 攻撃中は通常移動の入力処理へ進ませない
        if (CurrentState == PlayerActionState.Attack)
        {
            HandleAttackInput(input);
            return;
        }

        if (CurrentState == PlayerActionState.Dodge)
            return;

        if (!grounded)
        {
            CurrentState = PlayerActionState.Airborne;

            if (input.moveInput != Vector3.zero)
                playerMove.AirMove(input.moveInput);

            return;
        }

        // 着地後は通常行動へ戻す
        if (CurrentState == PlayerActionState.Airborne)
            CurrentState = PlayerActionState.Locomotion;

        if (input.DodgeInput)
        {
            StartDodge(input.moveInput);
            return;
        }

        if (input.GuardHold)
        {
            if (CurrentState != PlayerActionState.Guard)
            {
                CurrentState = PlayerActionState.Guard;
                actionPlayer.Play(timelineGroup, guardActionId);
            }

            return;
        }

        if (CurrentState == PlayerActionState.Guard)
            CurrentState = PlayerActionState.Locomotion;

        if (input.AttackInput)
        {
            StartAttack(1);
            return;
        }

        if (input.DushInput)
            playerMove.CanDush();

        playerMove.Move(input.moveInput);

        if (input.moveInput != Vector3.zero)
        {
            actionPlayer.Play(timelineGroup, moveActionId);
        }
        else
        {
            playerMove.DushEnd();
            actionPlayer.Play(timelineGroup, idleActionId);
        }
    }

    private void HandleAttackInput(IInputProvider input)
    {
        // 次段受付中のみ、次の攻撃へ進める
        if (input.AttackInput && comboWindowOpen)
        {
            int nextStep = attackStep + 1;

            if (nextStep <= attackActionIds.Length)
                StartAttack(nextStep);
        }

        // Timelineが許可した時間だけ回避キャンセル可能
        if (input.DodgeInput && dodgeCancelOpen)
        {
            StartDodge(input.moveInput);
        }
    }

    private void StartAttack(int step)
    {
        if (step < 1 || step > attackActionIds.Length)
            return;

        CurrentState = PlayerActionState.Attack;
        attackStep = step;

        // 前段の受付状態をリセット
        comboWindowOpen = false;
        dodgeCancelOpen = false;

        actionPlayer.ForcePlay(
            timelineGroup,
            attackActionIds[step - 1],
            1f,
            0.15f,
            false
        );
    }

    private void StartDodge(Vector3 moveInput)
    {
        CurrentState = PlayerActionState.Dodge;
        comboWindowOpen = false;
        dodgeCancelOpen = false;

        actionPlayer.Play(
            timelineGroup,
            dodgeActionId,
            1f,
            0.15f,
            false
        );

        playerMove.StartDodge(moveInput, 0.5f);
    }

    // 以下はTimeline Signalから呼び出す

    public void OpenComboWindow()
    {
        if (CurrentState != PlayerActionState.Attack)
            return;

        if (attackStep < attackActionIds.Length)
            comboWindowOpen = true;
    }

    public void CloseComboWindow()
    {
        comboWindowOpen = false;
    }

    public void OpenDodgeCancelWindow()
    {
        if (CurrentState == PlayerActionState.Attack)
            dodgeCancelOpen = true;
    }

    public void CloseDodgeCancelWindow()
    {
        dodgeCancelOpen = false;
    }

    public void EndAttack()
    {
        if (CurrentState != PlayerActionState.Attack)
            return;

        comboWindowOpen = false;
        dodgeCancelOpen = false;
        attackStep = 0;

        CurrentState = PlayerActionState.Locomotion;
    }

    public void EndDodge()
    {
        if (CurrentState != PlayerActionState.Dodge)
            return;

        CurrentState = PlayerActionState.Locomotion;
    }

    private void FixedUpdate()
    {
        if (!charaGround.GetGround())
            playerMove.ApplyGravity();
    }
}

