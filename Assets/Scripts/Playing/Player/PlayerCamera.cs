using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cfg = Config.Player.Camera;

public class PlayerCamera : MonoBehaviour {
	public PlayerController controller;

	public Camera Camera;

	internal bool FirstPerson = true;
	internal float tpDistance;
	float transSFov = 0;
	float tpDistTarget;

	float zoomT;
	float realZoom;
	bool zooming = false;


	void Start() {
		transSFov = 2 * cfg.R;
	}

	bool criterion => ContextManager.CurrentlyInContextStrict<Contexts.Playing>()
			|| (ContextManager.CurrentlyInContext<Contexts.Operating>()
			&& !ContextManager.CurrentlyInContext<Contexts.Operating.InCamera>());
	void Update() {
		if (!criterion) return;

		HandlePerspectiveZoom();

		UpdateCamera();
	}

	bool continueFovEffect = false;
	float smoothScroll;
	void HandlePerspectiveZoom() {
		// zoom

		zooming = Conatrols.IM.Playing_Camera.ZoomUse.WasPressedThisFrame() ? !zooming : zooming;

		float scroll = Conatrols.IM.Playing_Camera.ZoomAmount.ReadValue<float>();
		smoothScroll = Mathf.Lerp(smoothScroll, scroll, 5 * Time.deltaTime);

		if ((FirstPerson || continueFovEffect) && !zooming) {
			transSFov = Mathf.Lerp(transSFov, transSFov + smoothScroll * cfg.transitionScrollStrength, cfg.transitionFovSmoothing * Time.deltaTime);
			transSFov += resistance(transSFov);
			transSFov = Mathf.Clamp(transSFov, 0f, 2 * cfg.R);

			Camera.fieldOfView = Config.FOV + resistance(transSFov) * cfg.transitionFovEffect / cfg.L;

			if (transSFov < cfg.R && !continueFovEffect) {
				FirstPerson = false;
				continueFovEffect = true;
				tpDistance = 0;
				tpDistTarget = 0;
			}
		}
		if (!FirstPerson) {
			if (tpDistance < 0) {
				FirstPerson = true;
				transSFov = 2 * cfg.R;
				continueFovEffect = false;
			}

			if (continueFovEffect) {
				tpDistTarget -= resistance(transSFov) * cfg.transitionResidual / cfg.L;
				if (scroll > 0) // interrupt if go forward
					continueFovEffect = false;
			}

			float sensCoef = HF.ArbitrarySmoothStep(tpDistTarget, cfg.tpCloseFalloff.x, cfg.tpCloseFalloff.y, cfg.tpDistMin, 1, cfg.tpCloseFalloff.z, cfg.tpCloseFalloff.w);
			tpDistTarget -= scroll * cfg.tpScrollSensitivity * sensCoef;
			tpDistTarget = Mathf.Min(tpDistTarget, cfg.tpDistMax);
			tpDistance = Mathf.Lerp(tpDistance, tpDistTarget, cfg.tpDistSmooth * Time.deltaTime);

			if (Physics.Raycast(
				new(controller.Model.StickBase.position, (Camera.transform.position - controller.Model.StickBase.position).normalized),
				out var hit,
				Mathf.Infinity,
				~(1 << LayerMask.NameToLayer("Player"))))
				if (hit.distance < tpDistance + cfg.tpCollisionOutset)
					tpDistance = hit.distance - cfg.tpCollisionOutset;

			// tp extra fov
			Camera.fieldOfView = Config.FOV +
				Mathf.Pow(Mathf.InverseLerp(cfg.tpDistMin, cfg.tpDistMax, tpDistance), cfg.tpFovExtraPow) * cfg.tpOuterFovExtra;

			if (transSFov < 5) {
				continueFovEffect = false;
			}
		}

		if (zooming && FirstPerson) {
			zoomT += scroll * cfg.zoomSens;
			zoomT = Mathf.Clamp01(zoomT);

			float target = HF.Falloff(zoomT, cfg.zoomCurveSlope, 1, 1) * cfg.maxZoom;
			realZoom = Mathf.Lerp(realZoom, target, cfg.zoomSmooth * Time.deltaTime);

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
			LoverPeak = cfg.L / g(cfg.R + cfg.S * oneoversqrttwo);
			//precalculated = true;
		}

		return LoverPeak * g(x);
	}
	float g(float x) =>
		Mathf.Exp(-Mathf.Pow((x - cfg.R) / cfg.S, 2)) * (x - cfg.R);

	void UpdateCamera() {
		if (FirstPerson) {
			Camera.transform.SetPositionAndRotation(
				controller.Model.Eyes.position,
				Quaternion.Euler(controller.pitch, controller.yaw, 0));
		} else {
			Vector3 offset =
				Quaternion.Euler(controller.pitch, controller.yaw, 0)
				* -Vector3.forward
				* tpDistance;

			Vector3 pos = controller.Model.StickBase.position + offset;
			if (!FirstPerson && tpDistance < cfg.tpDistMin)
				pos = Vector3.Lerp(
					controller.Model.Eyes.position,
					pos,
					Mathf.InverseLerp(0, cfg.tpDistMin, tpDistance));

			Camera.transform.SetPositionAndRotation(
				pos,
				Quaternion.Euler(controller.pitch, controller.yaw, 0));
		}
	}
}