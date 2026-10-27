using System;
using UnityEngine;
using UnityEngine.Windows;

public enum PlayerActionState
{
    Locomotion, // 通常移動
    Guard,      // ガード
    Dodge,      // 回避
    Attack,     // 攻撃
    Charge,     // チャージ
    SpecialAttack,    // 特殊行動
}

[RequireComponent(typeof(PlayerMovement))]
public class PlayerActionController : MonoBehaviour
{
    #region 変数倉庫
    [Header("References")]
    [SerializeField] private TimelineActionPlayer actionPlayer;

    private PlayerMovement playerMove;
    private CharaGroundColDitection charaGround;
    private ActionEventReceiver eventReceiver;

    [Header("Timeline Action IDs")]
    [SerializeField] private string timelineGroup = "Player_1";
    [SerializeField] private int idleActionId = 0;
    [SerializeField] private int moveActionId = 1;
    [SerializeField] private int guardActionId = 3;
    [SerializeField] private int dodgeActionId = 4;
    [SerializeField] private int[] attackActionIds = { 2, 5, 6 };
    [SerializeField] private int specialActionId = 7;

    public PlayerActionState CurrentState { get; private set; }
        = PlayerActionState.Locomotion;

    private int attackStep;
    private bool comboWindowOpen;
    private bool dodgeCancelOpen;
    private string expectedPresetName;
    private int expectedActionId = -1;

    [Header("Attack Input Buffer")]
    [SerializeField, Min(0f)]
    private float attackBufferDuration = 0.2f;
    private float attackBufferExpireTime = -1f;
    private bool HasBufferedAttack =>
        attackBufferExpireTime >= Time.time;

    #endregion


    #region Unity Lifecycle
    private void Awake()
    {
        playerMove = GetComponent<PlayerMovement>();
        charaGround = GetComponentInChildren<CharaGroundColDitection>();
        eventReceiver = GetComponentInChildren<ActionEventReceiver>();
    }
    private void Start()
    {
        actionPlayer.Play(timelineGroup, idleActionId);
        
    }
    // TimelineActionPlayerのイベントを購読する
    private void OnEnable()
    {
        actionPlayer.PlaybackEnded += OnPlaybackEnded;

        if (eventReceiver != null)
            eventReceiver.ComboWindowChanged += OnComboWindowChanged;
    }

    // TimelineActionPlayerのイベント購読を解除する
    private void OnDisable()
    {
        actionPlayer.PlaybackEnded -= OnPlaybackEnded;

        if (eventReceiver != null)
            eventReceiver.ComboWindowChanged -= OnComboWindowChanged;
    }

    private void OnComboWindowChanged(bool open)
    {
        comboWindowOpen =
            CurrentState == PlayerActionState.Attack && open;
        //Debug.Log($"OnComboWindowChanged: {comboWindowOpen}");
    }

    // TimelineActionPlayerの再生を終了する
    private void OnPlaybackEnded(
        string presetName, 
        TimelineActionPreset.Entry entry)
    {
        //Debug.Log($"OnPlaybackEnded: {presetName}, {entry.id}");
        if (presetName != expectedPresetName ||
    entry.id != expectedActionId)
        {
            return;
        }

        if (CurrentState == PlayerActionState.Attack ||
            CurrentState == PlayerActionState.Dodge ||
            CurrentState == PlayerActionState.SpecialAttack)
        {
            CurrentState = PlayerActionState.Locomotion;

            comboWindowOpen = false;
            dodgeCancelOpen = false;
            attackStep = 0;

            ClearAttackBuffer();
        }
    }
    #endregion
    public void HandleInput(IInputProvider input)
    {
        bool grounded = charaGround.GetGround();

        if(grounded == false)
        {
            playerMove.AirMove(input.moveInput);
            return;
        }

        //Debug.Log($"HandleInput: {CurrentState}");

        // 攻撃中は攻撃専用の処理だけ行う
        if (CurrentState == PlayerActionState.Attack)
        {
            HandleAttackInput(input);
            return;
        }
        if(CurrentState == PlayerActionState.SpecialAttack)
        {
            return;
        }

        if (input.SpecialInput &&
        CurrentState != PlayerActionState.SpecialAttack)
        {
            StartSpecialAttack();
            return;
        }

        // 回避中も通常移動を実行しない
        if (CurrentState == PlayerActionState.Dodge)
        {
            return;
        }

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

        if(input.SpecialInput)
        {
            actionPlayer.ForcePlay(timelineGroup, specialActionId, 1f, 0.15f, false);
        }

        if (input.AttackInput && CurrentState != PlayerActionState.Attack)
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

    #region 攻撃中の入力処理

    private void HandleAttackInput(IInputProvider input)
    {
        playerMove.MoveStop();
        // 攻撃の先行入力は受付期間外でも保存する
        if (input.AttackInput)
        {
            
            BufferAttackInput();
        }
            
        // ここから下のキャンセル行動は
        // コンボ受付期間中だけ許可する
        if (comboWindowOpen)
        {
            // 回避キャンセル
            if (input.DodgeInput)
            {
                CancelAttack();
                StartDodge(input.moveInput);
                return;
            }

            // ガードキャンセル
            if (input.GuardHold)
            {
                CancelAttack();

                CurrentState = PlayerActionState.Guard;
                actionPlayer.ForcePlay(
                    timelineGroup,
                    guardActionId,
                    1f,
                    0.15f,
                    true
                );

                return;
            }

            // 移動キャンセル
            if (input.moveInput != Vector3.zero)
            {
                CancelAttack();

                playerMove.Move(input.moveInput);

                actionPlayer.ForcePlay(
                    timelineGroup,
                    moveActionId,
                    1f,
                    0.15f,
                    true
                );

                return;
            }

            // コンボ受付期間中に先行入力を消費
            if (HasBufferedAttack)
            {
                int nextStep = attackStep + 1;

                if (nextStep <= attackActionIds.Length)
                {
                    ClearAttackBuffer();
                    StartAttack(nextStep);
                }
            }
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
        // TimelineActionPlayerの再生終了を待つために、期待するPreset名とActionIdを設定
        expectedPresetName = timelineGroup;
        expectedActionId = attackActionIds[step - 1];

        actionPlayer.ForcePlay(
            timelineGroup,
            attackActionIds[step - 1],
            1f,
            0.15f,
            false
        );
    }
    private void CancelAttack()
    {
        comboWindowOpen = false;
        dodgeCancelOpen = false;

        // コンボ段数をリセット
        attackStep = 0;

        // 古い先行入力を持ち越さない
        ClearAttackBuffer();

        CurrentState = PlayerActionState.Locomotion;
    }
    private void StartDodge(Vector3 moveInput)
    {
        CurrentState = PlayerActionState.Dodge;
        comboWindowOpen = false;
        dodgeCancelOpen = false;

        // TimelineActionPlayerの再生終了を待つために、期待するPreset名とActionIdを設定
        expectedPresetName = timelineGroup;
        expectedActionId = dodgeActionId;

        actionPlayer.ForcePlay(
            timelineGroup,
            dodgeActionId,
            1f,
            0.15f,
            false
        );

        playerMove.StartDodge(moveInput, 0.5f);
    }

    private void StartSpecialAttack()
    {
        CurrentState = PlayerActionState.SpecialAttack;

        comboWindowOpen = false;
        dodgeCancelOpen = false;
        attackStep = 0;

        expectedPresetName = timelineGroup;
        expectedActionId = specialActionId;

        actionPlayer.ForcePlay(
            timelineGroup,
            specialActionId,
            1f,
            0.15f,
            false
        );
    }

    private void BufferAttackInput()
    {
        attackBufferExpireTime = Time.time + attackBufferDuration;
    }

    private void ClearAttackBuffer()
    {
        attackBufferExpireTime = -1f;
    }
    #endregion

    private void FixedUpdate()
    {
        if (!charaGround.GetGround())
            playerMove.ApplyGravity();
    }


}

