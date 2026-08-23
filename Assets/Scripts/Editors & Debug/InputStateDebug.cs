using UnityEngine;
using UnityEngine.InputSystem;

public class InputStateDebug : MonoBehaviour
{
    private PlayerInput playerInput;
    private Behaviour opsiveInput;

    private bool lastPlayerInput;
    private bool lastOpsiveInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        // Replace this with the concrete Opsive Unity Input System type if desired.
        foreach (var behaviour in GetComponents<Behaviour>())
        {
            if (behaviour.GetType().Name == "UnityInputSystem")
            {
                opsiveInput = behaviour;
                break;
            }
        }

        lastPlayerInput = playerInput != null && playerInput.enabled;
        lastOpsiveInput = opsiveInput != null && opsiveInput.enabled;
    }

    private void Update()
    {
        if (playerInput != null && playerInput.enabled != lastPlayerInput)
        {
            Debug.LogError(
                $"PlayerInput changed: {lastPlayerInput} -> {playerInput.enabled}"
            );

            lastPlayerInput = playerInput.enabled;
        }

        if (opsiveInput != null && opsiveInput.enabled != lastOpsiveInput)
        {
            Debug.LogError(
                $"Opsive input changed: {lastOpsiveInput} -> {opsiveInput.enabled}"
            );

            lastOpsiveInput = opsiveInput.enabled;
        }
    }
}