using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool interact;
		public bool useLeftHand;
		public bool useRightHand;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			Debug.Log("Move");
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}

		public void OnInteract(InputValue value)
		{
			if (!value.isPressed)
				return;
			
			InteractInput(true);
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

			GameManager.Instance.RequestRestart();
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


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

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
	
}