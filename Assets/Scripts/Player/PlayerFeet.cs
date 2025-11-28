using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFeet : MonoBehaviour {
	public PlayerController controller;

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

	public Transform leftFoot;
	public Transform rightFoot;

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

		public IEnumerator Step(PlayerFeet controller, Foot other, float forward, Vector3 joint, Vector3 up, Vector3 dir, Vector3 right) {
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

	public Foot Left = new();
	public Foot Right = new();

	Vector3 up;
	Vector3 lastPos;
	Vector3 lastNonZeroVel;
	Vector3 smoothVel;
	Vector3 dir;

	void Update() {
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