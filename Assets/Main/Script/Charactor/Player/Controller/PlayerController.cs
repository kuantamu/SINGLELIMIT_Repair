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
        //地上にいる場合の処理
        if (charaGround.GetGround())
        {
            //ジャンプ入力がある場合の処理
            //if (inputProvider.JumpInput)
            //{
            //    playerMove.Jump();
            //}
            if (inputProvider.DushInput)
            {
                playerMove.CanDush();
            }
            
            //移動入力がある場合の処理
            if (inputProvider.moveInput != Vector3.zero)
            {
                playerMove.Move(inputProvider.moveInput);
            }
            else
            {
                playerMove.Move(Vector3.zero);
                playerMove.DushEnd();
            }
        }
        else
        {
            //空中にいる場合の処理
            if (inputProvider.moveInput != Vector3.zero)
            {
                playerMove.AirMove(inputProvider.moveInput);
            }
        }
    }

    private void FixedUpdate()
    {
        //空中にいる場合の処理
        if (!charaGround.GetGround())
        {
            playerMove.ApplyGravity();
        }
    }
}
