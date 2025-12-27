using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cfg = Config.Player.Feet;

public class PlayerFeet : MonoBehaviour {
	public Transform leftFoot;
	public Transform rightFoot;

	public Transform SittingPosL;
	public Transform SittingPosR;

	public class Foot {
		public bool Grounded = true;
		public Vector3 Compare;
		public Vector3 Old;
		public Vector3 Target;
		public Vector3 Pos;
		public Quaternion Rot;
		public float t;
		public bool justWent = false;

		public void Check(PlayerFeet controller, Foot other, Vector3 joint, Vector3 dir, Vector3 right, Vector3 up) {
			if (Physics.Raycast(new(joint, -up), out var hit))
				Compare = hit.point;


			if ((Compare - Target).sqrMagnitude > cfg.maxDist * cfg.maxDist
				&& Vector3.Dot(dir, Target - Compare) < 0
				&& other.Grounded && Grounded && !justWent) {

				float speed = (Compare - Target).magnitude / cfg.maxDist;
				speed = Mathf.Pow(speed, cfg.distanceInfluence);

				float fwd = Mathf.Min(cfg.stepForward * speed, cfg.maxStepForward);

				controller.StartCoroutine(Step(other, fwd, joint, up, dir, right));

				justWent = true;
			}
		}

		public IEnumerator Step(Foot other, float forward, Vector3 joint, Vector3 up, Vector3 dir, Vector3 right) {
			Grounded = false;

			if (Physics.Raycast(
				new(joint + dir * forward, -up)
				, out var lnewhit)) {
				Old = Target;
				Target = lnewhit.point;
				t = 0;
			}

			while (t <= 1) {
				float ts = HF.VariableSmoothStep01(t, cfg.animSSin, cfg.animSSout);
				Pos = Curves.Bezier(new[] { Old, Old + up * cfg.stepUp, Target + up * cfg.stepUp, Target }, ts);

				// dont fix with angleaxis :|
				float lift = -4 * ts * (ts - 1) * cfg.angleUp;
				Rot = Quaternion.LookRotation(dir + Vector3.up * lift, up);

				// continuously update speed
				float speed = (Compare - Target).magnitude / cfg.maxDist;
				speed = Mathf.Pow(speed, cfg.distanceInfluence);

				t += Time.deltaTime * cfg.animSpeed * speed;
				yield return null;
			}

			Grounded = true;
			other.justWent = false;
		}
	}

	public Foot Left = new();
	public Foot Right = new();

	Vector3 up;
	Vector3 lastPos;
	Vector3 lastNonZeroVel;
	Vector3 smoothVel;
	Vector3 dir;

	void LateUpdate() {
		if (ContextManager.CurrentlyInContext<Contexts.Playing>(out var playing) && playing.Sitting) {
			leftFoot.SetPositionAndRotation(SittingPosL.position, SittingPosL.rotation);
			rightFoot.SetPositionAndRotation(SittingPosR.position, SittingPosR.rotation);
			return;
		}

		Vector3 vel = transform.position - lastPos;
		smoothVel = Vector3.Lerp(smoothVel, lastNonZeroVel, cfg.velSmoothing * Time.deltaTime);
		dir = smoothVel.normalized;

		if (Physics.Raycast(new(transform.position, -transform.up), out var hit)) {
			up = hit.normal;
		}
		Vector3 right = -Vector3.Cross(dir, up);

		Vector3 Ljoint = transform.position - right * cfg.hipWidth / 2f;
		Vector3 Rjoint = transform.position + right * cfg.hipWidth / 2f;

		Left.Check(this, Right, Ljoint, dir, right, up);
		Right.Check(this, Left, Rjoint, dir, right, up);

		lastPos = transform.position;

		if (vel.sqrMagnitude > 0)
			lastNonZeroVel = vel;

		leftFoot.SetPositionAndRotation(
			Left.Pos,
			Left.Rot
			);

		rightFoot.SetPositionAndRotation(
			Right.Pos,
			Right.Rot
			);
	}
}