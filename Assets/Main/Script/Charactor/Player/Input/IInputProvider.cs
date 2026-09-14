using UnityEngine;

public interface IInputProvider
{
    Vector3 moveInput { get; }
    bool AttackInput { get; }
    bool AttackHold { get; }
    bool GuardHold { get; }
    bool DodgeInput { get; }
    bool JumpInput { get; }
    bool SpecialInput { get; }
}
