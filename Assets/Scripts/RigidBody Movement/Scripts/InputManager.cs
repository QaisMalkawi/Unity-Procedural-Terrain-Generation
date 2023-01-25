using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
	[Header("Character Input Values")]
	public Vector2 move;
	public Vector2 look;

	public bool sprint, jump;

	[Header("Movement Settings")]
	public bool hasControl = true;

#if !UNITY_IOS || !UNITY_ANDROID
	[Header("Mouse Cursor Settings")]
	public bool cursorLocked = true;

	public bool invertX, invertY = false;
#endif


	public void OnMove(InputValue value)
	{
		if (hasControl)
		{
			MoveInput(value.Get<Vector2>());
		}
	}

	public void OnLook(InputValue value)
	{
		if (hasControl)
		{
			LookInput(value.Get<Vector2>());
		}
	}
	public void OnSprint(InputValue value)
	{
		if (hasControl)
		{
			SprintInput(value.Get<float>());
		}
	}
	public void OnJump(InputValue value)
	{
		if (hasControl)
		{
			JumpInput();
		}
	}
	public void OnToggleControl(InputValue value)
	{
		ToggleControl();
	}


	public void ToggleControl()
	{
		hasControl = !hasControl;

		cursorLocked = !cursorLocked;
		Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.Confined;

		if (!hasControl)
		{
			move = look = Vector2.zero;
		}
	}
	public void MoveInput(Vector2 newMoveDirection)
	{
		move = newMoveDirection;
	}
	public void LookInput(Vector2 newLookDirection)
	{
		look.x = newLookDirection.x * (invertX ? -1 : 1);
		look.y = newLookDirection.y * (invertY ? 1 : -1);
	}
	public void SprintInput(double newSprintState)
	{
		sprint = newSprintState == 0.0 ? false : true;
	}
	public void JumpInput()
	{
		jump = true;
	}
}