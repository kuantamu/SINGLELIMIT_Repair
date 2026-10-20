using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    private IInputProvider inputProvider;
    private PlayerActionController actionController;

    private void Awake()
    {
        inputProvider = GetComponent<IInputProvider>();
        actionController = GetComponent<PlayerActionController>();
    }

    private void Update()
    {
        actionController.HandleInput(inputProvider);
    }
}
