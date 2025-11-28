using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour{
	public float Speed;
	public PlayerCameraManager visuals;

	public Vector2 fpPitchLimits;
	public Vector2 tpPitchLimits;

	public float Sensitivity;

	
	public Transform HoldingTransform;

	Rigidbody rb;

	[HideInNormalInspector] public float yaw;
	[HideInNormalInspector] public float pitch;
	[HideInNormalInspector] public Vector3 movement;
	
	void Start() {
		rb = GetComponent<Rigidbody>();

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void Update() {
		Vector2 input = Conatrols.IM.Player.Move.ReadValue<Vector2>();

		movement =
			Quaternion.Euler(0, yaw, 0)
			* new Vector3(input.x, 0, input.y).normalized
			* Speed;

		rb.velocity = movement + rb.velocity.y * Vector3.up;

		yaw += Conatrols.Mouse.Delta.x * Sensitivity;
		pitch -= Conatrols.Mouse.Delta.y * Sensitivity;

		pitch = Mathf.Clamp(pitch,
			visuals.FirstPerson ? fpPitchLimits.x : tpPitchLimits.x,
			visuals.FirstPerson ? fpPitchLimits.y : tpPitchLimits.y);
	}
}