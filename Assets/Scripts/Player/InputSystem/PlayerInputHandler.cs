using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using GameEvents;
using UnityEngine.InputSystem;
#endif

public class PlayerInputHandler : MonoBehaviour
{
	[Header("Character Input Values")]
	public bool interact;
	public bool useLeftHand;
	public bool useRightHand;

#if ENABLE_INPUT_SYSTEM
	public void OnInteract(InputValue value)
	{
		// action = interaction
		if (!value.isPressed)
			return;
		
		InteractInput(true);
	}
	
	public void OnOpenInventory(InputValue value)
	{
		if (!value.isPressed)
			return;
		
		Debug.Log("Open Inventory!!!");
		
		GameEventManager.Raise(new ToggleInventoryEvent());
	}
	
	public void OnPause(InputValue value)
	{
		if (!value.isPressed)
			return;
		
		GameManager.Instance.TogglePause();
	}

	public void OnRestart(InputValue value)
	{
		if (!value.isPressed)
			return;

		GameManager.Instance.RestartRequest();
	}

	public void OnUseLeftHand(InputValue value)
	{
		if (!value.isPressed)
			return;
		
		UseLeftHandInput(true);
	}

	public void OnUseRightHand(InputValue value)
	{
		if (!value.isPressed)
			return;
		
		UseRightHandInput(true);
	}
#endif
	
	public void InteractInput(bool newInteractState)
	{
		interact = newInteractState;
	}

	public void UseLeftHandInput(bool newInteractState)
	{
		Debug.Log("Left Hand");
		useLeftHand = newInteractState;
	}

	public void UseRightHandInput(bool newInteractState)
	{
		Debug.Log("Right Hand");
		useRightHand = newInteractState;
	}
}