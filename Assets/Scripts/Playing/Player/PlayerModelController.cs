using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cfg = Config.Player.Camera;

public class PlayerModelController : MonoBehaviour {
	public PlayerController Controller;
	public PlayerFeet Feet;
	public PlayerBeak Beak;

	public Transform Neck;
	public Transform Eyes;
	public Transform StickBase;
	public Transform BasePosL;
	public Transform BasePosR;
	public Transform Stick;
	public GameObject FaceGroup;

	float yawFreedom; // angle l and r before body rotates too
	float bodyYaw;
	float smoothedBodyYaw;

	PlayerCameraController Camera;

	void Start() {
		Camera = Controller.Camera;
	}

	void Update() {
		FaceGroup.SetActive(Camera.tpDistance > cfg.hideFaceTpDist);

		Neck.localRotation = Quaternion.Euler(Controller.pitch * (Camera.FirstPerson ? 1 : cfg.tpNeckRotationCoef), 0, 0);

		// constrain camera rotation to limits, rotate body
		yawFreedom =
			Camera.FirstPerson
			? cfg.firstPersonYawFreedom
			: cfg.thirdPersonYawFreedom;

		float diff = Controller.yaw - bodyYaw;
		diff = (diff + 180) % 360 - 180;
		if (diff < -yawFreedom)
			bodyYaw -= -diff - yawFreedom;
		if (diff > yawFreedom)
			bodyYaw += diff - yawFreedom;

		if (Controller.movement.sqrMagnitude > 0)
			bodyYaw = HF.AngleLerp(bodyYaw, Controller.yaw, cfg.movingYawMatchSmooth * Time.deltaTime);

		smoothedBodyYaw = HF.AngleLerp(smoothedBodyYaw, bodyYaw, cfg.bodyYawSmooth * Time.deltaTime);
		transform.rotation = Quaternion.Euler(0, smoothedBodyYaw, 0);

		bool tpLeft = HF.AngleDiff(Controller.yaw, smoothedBodyYaw) > 0;

		//Stick.localRotation = Quaternion.Euler(90, tpLeft ? -StickAngle : StickAngle, 0);

		StickBase.position =
			tpLeft
			? BasePosL.position
			: BasePosR.position;

		StickBase.LookAt(Camera.Camera.transform);
		StickBase.localScale = new(1, 1, Camera.tpDistance);

		if (!Camera.FirstPerson)
			Camera.CameraTransform.position +=
				Mathf.InverseLerp(0, cfg.tpDistMin, Camera.tpDistance) *
				(tpLeft ? -1 : 1) * cfg.stickCameraOffset * Stick.right;
	}
}