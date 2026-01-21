using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cfg = Config.Player.Controller;

public class PlayerController : MonoBehaviour{
	public PlayerCamera Camera;
	public PlayerModel Model;
	public Transform HoldingTransform; // separate holding out too if needed i guess


	[HideInNormalInspector] public float yaw;
	[HideInNormalInspector] public float pitch;
	[HideInNormalInspector] public Vector3 movement;
	
	internal Rigidbody rb;

	// whole sitting system might have to be redone
	internal Part_Seat CurrentlySittingOn;
	FixedJoint SitJoint;
	
	void Start() {
		rb = GetComponent<Rigidbody>();

		Conatrols.IM.Playing_Player.Sit.Subscribe<Contexts.Playing>(HandleSitting);
		Conatrols.IM.Playing_Player.Operate.Subscribe<Contexts.Playing>(OperateControlPressed, true);

		GameManager.Instance.PC_AutoSit = AutoSit;
		GameManager.Instance.PC_Unsit = Unsit;
	}

	void Update() {
		if (ContextManager.CurrentlyInContext<Contexts.Playing>(out var playing)
			&& !ContextManager.CurrentlyInContext<Contexts.Editing>()
			&& !playing.Sitting
			&& !GameManager.Instance.Paused)
			HandleMovement();

		if (ContextManager.CurrentlyInContext<Contexts.Playing>()
			&& !ContextManager.CurrentlyInContext<Contexts.Editing>()
			&& !GameManager.Instance.Paused)
			HandleCamera();
	}

	void HandleMovement() {
		Vector2 input = Conatrols.IM.Playing_Player.Move.ReadValue<Vector2>();

		movement =
			Quaternion.Euler(0, yaw, 0)
			* new Vector3(input.x, 0, input.y).normalized
			* cfg.Speed;

		rb.velocity = movement + rb.velocity.y * Vector3.up;
	}

	void HandleCamera() {
		if (ContextManager.CurrentlyInContextStrict<Contexts.Playing>()) {
			yaw += Conatrols.Mouse.Delta.x * cfg.Sensitivity;
			pitch -= Conatrols.Mouse.Delta.y * cfg.Sensitivity;
		} else
		if (Conatrols.IM.Operating_Player.RotateCamera.IsPressed()) {
			yaw += Conatrols.Mouse.Delta.x * cfg.OperatingSensitivity;
			pitch -= Conatrols.Mouse.Delta.y * cfg.OperatingSensitivity;
		}

		pitch = Mathf.Clamp(pitch,
			Camera.FirstPerson ? cfg.FirstPersonPitchLimits.x : cfg.ThirdPersonPitchLimits.x,
			Camera.FirstPerson ? cfg.FirstPersonPitchLimits.y : cfg.ThirdPersonPitchLimits.y);
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

	void AutoSit() {
		StartCoroutine(AutoSitDelayed());
	}

	IEnumerator AutoSitDelayed() {
		yield return null;

		// find first seat in creation hiearchy

		var firstseatassembledpart = OperatingManager.Instance.CurrentlyOperating
			.Construct.Parts.FirstOrDefault(p => p is Part_Seat.CPart);

		if (firstseatassembledpart == null) yield break;

		var firstseatid = firstseatassembledpart.id;

		// wah wah performance\
		// i genuinely dont understand this myself either im ngl.
		var firstseat =
			OperatingManager.Instance.CurrentlyOperating
			.SubAssemblies.Select(sa => sa.Parts.FirstOrDefault(p => p.ID == firstseatid))
			.FirstOrDefault(s => s != null).GetNSP<Part_Seat>();

		if (firstseat == null) yield break;

		CurrentlySittingOn = firstseat;
		SetupSit();
	}

	void TrySit() {
		Part_Seat targetedSeat = PlayingManager.Instance.TargetedSeat;
		if (targetedSeat == null) return;

		CurrentlySittingOn = targetedSeat;

		SetupSit();
	}

	// expects currentlysittingon to be set
	internal void SetupSit() {
		if (CurrentlySittingOn == null) return;

		// place player's bum at sit target pos

		if (SitJoint != null)
			Unsit();

		Vector3 offset = CurrentlySittingOn.SitTarget.position - Model.Bum.position;
		transform.position += offset;

		// fixed joint..

		//SitJoint = CurrentlySittingOn.transform.parent.gameObject.AddComponent<FixedJoint>();
		//SitJoint.connectedBody = rb;

		SitJoint = gameObject.AddComponent<FixedJoint>();
		SitJoint.connectedBody = CurrentlySittingOn.transform.parent.GetComponent<Rigidbody>();

		rb.velocity = Vector3.zero;
		rb.freezeRotation = false;

		ContextManager.GetCurrent<Contexts.Playing>().Sitting = true;
	}

	internal void Unsit() {
		if (SitJoint == null) return;

		// try raycast
		if (Physics.Raycast(
			new(Camera.Camera.transform.position, Camera.Camera.transform.forward),
			out var hit,
			Config.Player.Behaviour.UnsitRaycastMaxDist)) {
			transform.position += hit.point - Model.Bum.position;
		} else {
			Vector3 dir = Camera.Camera.transform.forward;
			dir.y = 0;
			dir.Normalize();
			transform.position += dir * Config.Player.Behaviour.UnsitDistance;
		}

		// break the joint
		Destroy(SitJoint);
		SitJoint = null;

		CurrentlySittingOn = null;

		rb.freezeRotation = true;
		transform.rotation = Quaternion.identity;

		ContextManager.GetCurrent<Contexts.Playing>().Sitting = false;
	}

	void OperateControlPressed() {
		if (PlayingManager.Instance.TargetedCreation != null
			&& OperatingManager.Instance.CurrentlyOperating == null) {
			OperatingManager.Instance.CurrentlyOperating = PlayingManager.Instance.TargetedCreation;
			GameManager.Instance.Operate();
		}
	}
}