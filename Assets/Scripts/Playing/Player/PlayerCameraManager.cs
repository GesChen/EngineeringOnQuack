using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraManager : MonoBehaviour {
	public PlayerController controller;

	public Transform Neck;
	public Transform Eyes;
	public Transform StickBase;
	public Transform BasePosL;
	public Transform BasePosR;
	public Transform CameraTransform;
	public Camera Camera;
	public Transform Stick; 
	public float StickAngle;
	public float stickCameraOffset;
	public GameObject FaceGroup;

	[Header("perspective")]
	public bool FirstPerson;
	public float L = 2;
	public float R = 12;
	public float S = 3;
	public float transitionScrollStrength = 5;
	public float transitionFovSmoothing = 1;
	public float transitionFovEffect = 2;
	public float transitionResidual = 2;
	public float tpDistMin = 1;
	public float tpDistMax = 10;
	public float tpOuterFovExtra = 5;
	public float tpFovExtraPow = 10;
	public float tpScrollSensitivity;
	public float tpDistSmooth = 5;
	public float tpNeckRotationCoef;
	public float tpCollisionOutset;
	public float hideFaceTpDist;

	public Vector4 tpCloseFalloff;

	float transSFov = 0;

	float tpDistTarget;
	float tpDistance;

	[Header("Zoom")]
	public float zoomSens;
	public float zoomCurveSlope;
	public float maxZoom;
	public float zoomSmooth;
	float zoomT;
	float realZoom;
	bool zooming = false;

	[Header("Body")]
	float yawFreedom; // angle l and r before body rotates too
	public float firstPersonYawFreedom;
	public float thirdPersonYawFreedom;
	public float movingYawMatchSmooth;
	public float bodyYawSmooth;

	float bodyYaw;
	float smoothedBodyYaw;

	void Start() {
		transSFov = 2 * R;
	}

	bool criterion => ContextManager.CurrentlyInContextStrict<Contexts.Playing>()
			|| (ContextManager.CurrentlyInContext<Contexts.Operating>()
			&& !ContextManager.CurrentlyInContext<Contexts.Operating.InCamera>());
	void Update() {
		if (!criterion) return;

		HandlePerspectiveZoom();

		UpdateCamera();

		UpdateModel();
	}

	bool continueFovEffect = false;
	float smoothScroll;
	void HandlePerspectiveZoom() {
		// zoom

		zooming = Conatrols.IM.Playing_Camera.ZoomUse.WasPressedThisFrame() ? !zooming : zooming;
		
		float scroll = Conatrols.IM.Playing_Camera.ZoomAmount.ReadValue<float>();
		smoothScroll = Mathf.Lerp(smoothScroll, scroll, 5 * Time.deltaTime);
		
		if ((FirstPerson || continueFovEffect) && !zooming) {
			transSFov = Mathf.Lerp(transSFov, transSFov + smoothScroll * transitionScrollStrength, transitionFovSmoothing * Time.deltaTime);
			transSFov += resistance(transSFov);
			transSFov = Mathf.Clamp(transSFov, 0f, 2 * R);

			Camera.fieldOfView = Config.FOV + resistance(transSFov) * transitionFovEffect / L;

			if (transSFov < R && !continueFovEffect) {
				FirstPerson = false;
				continueFovEffect = true;
				tpDistance = 0;
				tpDistTarget = 0;
			}
		} 
		if (!FirstPerson) {
			if (tpDistance < 0) {
				FirstPerson = true;
				transSFov = 2 * R;
				continueFovEffect = false;
			}

			if (continueFovEffect) {
				tpDistTarget -= resistance(transSFov) * transitionResidual / L;
				if (scroll > 0) // interrupt if go forward
					continueFovEffect = false;
			}

			float sensCoef = HF.ArbitrarySmoothStep(tpDistTarget, tpCloseFalloff.x, tpCloseFalloff.y, tpDistMin, 1, tpCloseFalloff.z, tpCloseFalloff.w);
			tpDistTarget -= scroll * tpScrollSensitivity * sensCoef;
			tpDistTarget = Mathf.Min(tpDistTarget, tpDistMax);
			tpDistance = Mathf.Lerp(tpDistance, tpDistTarget, tpDistSmooth * Time.deltaTime);

			if (Physics.Raycast(
				new(StickBase.position, (Camera.transform.position - StickBase.position).normalized),
				out var hit,
				Mathf.Infinity,
				~(1 << LayerMask.NameToLayer("Player"))))
				if (hit.distance < tpDistance + tpCollisionOutset)
					tpDistance = hit.distance - tpCollisionOutset;

			// tp extra fov
			Camera.fieldOfView = Config.FOV +
				Mathf.Pow(Mathf.InverseLerp(tpDistMin, tpDistMax, tpDistance), tpFovExtraPow) * tpOuterFovExtra;

			if (transSFov < 5) {
				continueFovEffect = false;
			}
		}

		if (zooming && FirstPerson) {
			zoomT += scroll * zoomSens;
			zoomT = Mathf.Clamp01(zoomT);

			float target = HF.Falloff(zoomT, zoomCurveSlope, 1, 1) * maxZoom;
			realZoom = Mathf.Lerp(realZoom, target, zoomSmooth * Time.deltaTime);

			Camera.fieldOfView = Config.FOV - realZoom;
			
			if (scroll < 0 && zoomT < .0001f) {
				zooming = false;
			}
		} else {
			zoomT = 0;
			realZoom = 0;
		}
	}

	float oneoversqrttwo = 0.707106781187f;
	bool precalculated = false;
	float LoverPeak; // l / peak (g(r+W))
	float resistance(float x) {
		if (!precalculated) {
			LoverPeak = L / g(R + S * oneoversqrttwo);
			//precalculated = true;
		}

		return LoverPeak * g(x);
	}
	float g(float x) => 
		Mathf.Exp(-Mathf.Pow((x - R) / S, 2)) * (x - R);

	void UpdateCamera() {
		if (FirstPerson) {
			CameraTransform.SetPositionAndRotation(
				Eyes.position, 
				Quaternion.Euler(controller.pitch, controller.yaw, 0));
		} else {
			Vector3 offset =
				Quaternion.Euler(controller.pitch, controller.yaw, 0)
				* -Vector3.forward
				* tpDistance;

			Vector3 pos = StickBase.position + offset;
			if (!FirstPerson && tpDistance < tpDistMin)
				pos = Vector3.Lerp(
					Eyes.position,
					pos,
					Mathf.InverseLerp(0, tpDistMin, tpDistance));

			CameraTransform.SetPositionAndRotation(
				pos, 
				Quaternion.Euler(controller.pitch, controller.yaw, 0));
		}
	}

	void UpdateModel() {
		FaceGroup.SetActive(tpDistance > hideFaceTpDist);

		Neck.localRotation = Quaternion.Euler(controller.pitch * (FirstPerson ? 1 : tpNeckRotationCoef), 0, 0);

		// constrain camera rotation to limits, rotate body
		yawFreedom =
			FirstPerson
			? firstPersonYawFreedom
			: thirdPersonYawFreedom;

		float diff = controller.yaw - bodyYaw;
		diff = (diff + 180) % 360 - 180;
		if (diff < -yawFreedom)
			bodyYaw -= -diff - yawFreedom;
		if (diff > yawFreedom)
			bodyYaw += diff - yawFreedom;

		if (controller.movement.sqrMagnitude > 0)
			bodyYaw = HF.AngleLerp(bodyYaw, controller.yaw, movingYawMatchSmooth * Time.deltaTime);

		smoothedBodyYaw = HF.AngleLerp(smoothedBodyYaw, bodyYaw, bodyYawSmooth * Time.deltaTime);
		transform.rotation = Quaternion.Euler(0, smoothedBodyYaw, 0);

		bool tpLeft = HF.AngleDiff(controller.yaw, smoothedBodyYaw) > 0;

		//Stick.localRotation = Quaternion.Euler(90, tpLeft ? -StickAngle : StickAngle, 0);

		StickBase.position =
			tpLeft
			? BasePosL.position
			: BasePosR.position;

		StickBase.LookAt(Camera.transform);
		StickBase.localScale = new(1, 1, tpDistance);

		if (!FirstPerson)
			Camera.transform.position +=
				Mathf.InverseLerp(0, tpDistMin, tpDistance) *
				(tpLeft ? -1 : 1) * stickCameraOffset * Stick.right;
	}
}