using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    private IInputProvider inputProvider;
    private PlayerMovement playerMove;
    private CharaGroundColDitection charaGround;

    private void Awake()
    {
        inputProvider = GetComponent<IInputProvider>();
        playerMove = GetComponent<PlayerMovement>();
        charaGround = GetComponentInChildren<CharaGroundColDitection>();
    }
    private void Update()
    {
        if (!charaGround.GetGround())
        {
            if (inputProvider.AttackInput)
            {
                playerMove.RotationCameraFoward();
                playerMove.AttackHop();
                return;
            }

            if (inputProvider.GuardHold && inputProvider.moveInput != Vector3.zero)
            {
                playerMove.StartDodge(inputProvider.moveInput, 0.5f);
                return;
            }
            return;
        }

        if (inputProvider.JumpInput)
        {
            
        }

        if (inputProvider.moveInput != Vector3.zero)
        {
            if (inputProvider.GuardHold)
            {

                playerMove.StartDodge(inputProvider.moveInput, 0.5f);
                return;
            }

            return;
        }

        if (inputProvider.AttackHold)
        {
            //攻撃状態に遷移
            playerMove.RotationCameraFoward();
            return;
        }

        if (inputProvider.GuardHold)
        {
            //ガード状態に遷移
            return;
        }

        //普通状態に戻す
    }
}
