using UnityEngine;

public class PlayerInputHandler : MonoBehaviour, IInputProvider
{
    public Vector3 moveInput { get; private set; }
    public bool DushInput { get; private set; }
    public bool AttackInput { get; private set; }
    public bool AttackHold { get; private set; }
    public bool GuardHold { get; private set; }
    public bool DodgeInput { get; private set; }
    public bool JumpInput { get; private set; }
    public bool SpecialInput { get; private set; }



    private void Update()
    {
        moveInput = MoveInputs();
        DushInput = Input.GetKey(KeyCode.LeftShift);
        AttackInput = Input.GetMouseButtonDown(0);
        AttackHold = Input.GetMouseButton(0);
        GuardHold = Input.GetMouseButton(1);
        DodgeInput = Input.GetMouseButton(1) && MoveInputs() != Vector3.zero;
        JumpInput = Input.GetKey(KeyCode.Space);
        SpecialInput = Input.GetKey(KeyCode.E);

    }

    private Vector3 MoveInputs()
    {
        Vector3 vec = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) vec.z += 1;
        if (Input.GetKey(KeyCode.S)) vec.z -= 1;
        if (Input.GetKey(KeyCode.A)) vec.x -= 1;
        if (Input.GetKey(KeyCode.D)) vec.x += 1;
        vec.Normalize();
        return vec;
    }
}
