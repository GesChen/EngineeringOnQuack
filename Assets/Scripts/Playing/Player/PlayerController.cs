using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cfg = Config.Player.Controller;

public class PlayerController : MonoBehaviour{
	public PlayerCameraController Camera;
	public PlayerModelController Model;
	public Transform HoldingTransform; // separate holding out too if needed i guess

	Rigidbody rb;

	[HideInNormalInspector] public float yaw;
	[HideInNormalInspector] public float pitch;
	[HideInNormalInspector] public Vector3 movement;

	// whole sitting system might have to be redone
	Part_Seat CurrentlySittingOn;
	
	void Start() {
		rb = GetComponent<Rigidbody>();

		Conatrols.IM.Playing_Player.Sit.Subscribe<Contexts.Playing>(HandleSitting);
	}

	void Update() {
		if (!(ContextManager.CurrentlyInContextStrict<Contexts.Playing>()
			|| ContextManager.CurrentlyInContext<Contexts.Operating>(out var operating)
			&& !(operating.Parent as Contexts.Playing).Sitting)) return;

		Vector2 input = Conatrols.IM.Playing_Player.Move.ReadValue<Vector2>();

		movement =
			Quaternion.Euler(0, yaw, 0)
			* new Vector3(input.x, 0, input.y).normalized
			* cfg.Speed;

		rb.velocity = movement + rb.velocity.y * Vector3.up;

		if (ContextManager.CurrentlyInContextStrict<Contexts.Playing>()) {
			yaw += Conatrols.Mouse.Delta.x * cfg.Sensitivity;
			pitch -= Conatrols.Mouse.Delta.y * cfg.Sensitivity;
		}else 
		if (Conatrols.IM.Operating_Player.RotateCamera.IsPressed()) {
			yaw += Conatrols.Mouse.Delta.x * cfg.OperatingSensitivity;
			pitch -= Conatrols.Mouse.Delta.y * cfg.OperatingSensitivity;
		}

		pitch = Mathf.Clamp(pitch,
			Camera.FirstPerson ? cfg.FirstPersonPitchLimits.x : cfg.ThirdPersonPitchLimits.x,
			Camera.FirstPerson ? cfg.FirstPersonPitchLimits.y : cfg.ThirdPersonPitchLimits.y);

		HandleSitting();
	}

	void HandleSitting() {
		bool sitting = ContextManager.CurrentlyInContext<Contexts.Playing>(out var playing)
			&& playing.Sitting;

		if (sitting) {
			Unsit();
		} else {
			TrySit();
		}
	}

	void TrySit() {
		Part_Seat targetedSeat = PlayingManager.Instance.TargetedSeat;
		if (targetedSeat == null) return;

		CurrentlySittingOn = targetedSeat;

		(ContextManager.Current as Contexts.Playing).Sitting = true;
	}

	void SetupSit() {

	}

	void Unsit() {
		// figure out a way to get up idk

		(ContextManager.Current as Contexts.Playing).Sitting = false;
	}
}