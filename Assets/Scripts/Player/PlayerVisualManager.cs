using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVisualManager : MonoBehaviour {
	public PlayerController controller;

	[Header("Feet")]
	public float maxDist = .5f;
	public float stepForward = .5f;
	public float maxStepForward = 1;
	public float distanceInfluence = .5f;
	public float stepUp = 1f;
	public float angleUp = .5f;
	public float hipWidth = 1;

	public float animSpeed = 5f;
	public float animSSin = 1.5f;
	public float animSSout = 4;
	public float velSmoothing = 1f;

	[Header("Camera")]
	public Transform Neck;
	public Transform Eyes;
	public Transform StickBase;
	public Transform CameraTransform;
	public Camera Camera;

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

	public Vector4 tpCloseFalloff;

	float transFovAdjust = 0;
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

	[Header("Model")]
	public Transform leftFoot;
	public Transform rightFoot;

	class Foot {
		public bool Grounded = true;
		public Vector3 Compare;
		public Vector3 Old;
		public Vector3 Target;
		public Vector3 Pos;
		public Quaternion Rot;
		public float t;
		public bool justWent = false;

		public void Check(PlayerVisualManager controller, Foot other, Vector3 joint, Vector3 dir, Vector3 right, Vector3 up) {
			if (Physics.Raycast(new(joint, -up), out var hit))
				Compare = hit.point;


			if ((Compare - Target).sqrMagnitude > controller.maxDist * controller.maxDist
				&& Vector3.Dot(dir, Target - Compare) < 0
				&& other.Grounded && Grounded && !justWent) {

				float speed = (Compare - Target).magnitude / controller.maxDist;
				speed = Mathf.Pow(speed, controller.distanceInfluence);

				float fwd = Mathf.Min(controller.stepForward * speed, controller.maxStepForward);
				
				controller.StartCoroutine(Step(controller, other, fwd, joint, up, dir, right));

				justWent = true;
			}
		}

		public IEnumerator Step(PlayerVisualManager controller, Foot other, float forward, Vector3 joint, Vector3 up, Vector3 dir, Vector3 right) {
			Grounded = false;

			if (Physics.Raycast(
				new(joint + dir * forward, -up)
				, out var lnewhit)) {
				Old = Target;
				Target = lnewhit.point;
				t = 0;
			}

			while (t <= 1) {
				float ts = HF.VariableSmoothStep01(t, controller.animSSin, controller.animSSout);
				Pos = Curves.Bezier(new[] { Old, Old + up * controller.stepUp, Target + up * controller.stepUp, Target }, ts);

				// dont fix with angleaxis :|
				float lift = -4 * ts * (ts - 1) * controller.angleUp;
				Rot = Quaternion.LookRotation(dir + Vector3.up * lift, up);

				// continuously update speed
				float speed = (Compare - Target).magnitude / controller.maxDist;
				speed = Mathf.Pow(speed, controller.distanceInfluence);

				t += Time.deltaTime * controller.animSpeed * speed;
				yield return null;
			}

			Grounded = true;
			other.justWent = false;
		}
	}

	Foot Left = new();
	Foot Right = new();

	float bodyYaw;

	Vector3 up;
	Vector3 lastPos;
	Vector3 lastNonZeroVel;
	Vector3 smoothVel;
	Vector3 dir;


	void Start() {
		transFovAdjust = 2 * R;
		transSFov = 2 * R;
	}

	void Update() {
		HandlePerspectiveZoom();

		UpdateFeet();

		UpdateCamera();

		UpdateModel();
	}


	bool continueFovEffect = false;
	float smoothScroll;
	void HandlePerspectiveZoom() {
		// zoom

		zooming = Conatrols.IM.PlayerCamera.ZoomUse.WasPressedThisFrame() ? !zooming : zooming;
		
		float scroll = Conatrols.IM.PlayerCamera.ZoomAmount.ReadValue<float>();
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
				transFovAdjust = 2 * R;
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

	void UpdateFeet() {
		Vector3 vel = transform.position - lastPos;
		smoothVel = Vector3.Lerp(smoothVel, lastNonZeroVel, velSmoothing * Time.deltaTime);
		dir = smoothVel.normalized;

		if (Physics.Raycast(new(transform.position, -transform.up), out var hit)) {
			up = hit.normal;
		}
		Vector3 right = -Vector3.Cross(dir, up);

		Vector3 Ljoint = transform.position - right * hipWidth / 2f;
		Vector3 Rjoint = transform.position + right * hipWidth / 2f;

		Left.Check(this, Right, Ljoint, dir, right, up);
		Right.Check(this, Left, Rjoint, dir, right, up);

		lastPos = transform.position;

		if (vel.sqrMagnitude > 0)
			lastNonZeroVel = vel;
	}

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
			bodyYaw = Mathf.Lerp(bodyYaw, controller.yaw, movingYawMatchSmooth * Time.deltaTime);

		transform.rotation = Quaternion.Euler(0, bodyYaw, 0);
	}

	void UpdateModel() {
		leftFoot.SetPositionAndRotation(
			Left.Pos,
			Left.Rot
			);

		rightFoot.SetPositionAndRotation(
			Right.Pos,
			Right.Rot
			);

		StickBase.LookAt(Camera.transform);
		StickBase.localScale = new(1, 1, tpDistance);
	}
}