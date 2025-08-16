using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;

public class CableObject : MonoBehaviour
{
	[Header("Config")]
	public Transform from;
	public Transform to;
	public int segments;
	public int extensionSegments;
	public float segmentMass;
	public float segmentDrag;
	public GameObject spanningObject;

	private List<Rigidbody> interRbs;

	[Header("Rendering")]
	public float radius;
	public float resolution;
	public Material cableMaterial;
	private LineRenderer lineRenderer;
	//private Mesh mesh;

	[Header("Stability")]
	public bool attemptStability;
	public bool spazWarning;
	public float maxVelocity;
	public float dragExponent;
	public float returnSpeed;

	[Header("Fun")] // may be removed
	public bool breakable;
	public float breakingForce;

	[Space]
	public Transform fromObject;
	public Transform toObject;

	void Start()
	{
		SetupCable();
	}

	void Update()
	{
		RenderCable();
		if(attemptStability) PreventSpazzing();
	}
	void SetupCable()
	{
		ResetCable();

		interRbs = new();

		// extra first as child of the start
		GameObject first = Instantiate(spanningObject, transform);
		first.name = "firstobj";
		fromObject = first.transform;
		first.transform.position = from.position;
		Rigidbody firstrb = first.GetComponent<Rigidbody>();
		firstrb.isKinematic = true;
		interRbs.Add(firstrb);

		for (int i = 0; i < segments + 1 + extensionSegments; i++)
		{
			GameObject inter = Instantiate(spanningObject, transform);
			float phase = (i + 1f) / (segments + 1f);
			inter.transform.position = Vector3.LerpUnclamped(from.position, to.position, phase);

			interRbs.Add(inter.GetComponent<Rigidbody>());
		}

		// last segment 
		GameObject last = Instantiate(spanningObject, transform);
		Destroy(last.GetComponent<ConfigurableJoint>());
		last.name = "lastobj";
		toObject = last.transform;
		last.transform.position = to.position;
		Rigidbody lastrb = last.GetComponent<Rigidbody>();
		lastrb.isKinematic = true;
		interRbs.Add(lastrb);

		for (int i = 0; i < interRbs.Count - 1; i++)
		{
			Rigidbody cur = interRbs[i];
			Rigidbody next = interRbs[i + 1];

			ConfigurableJoint joint = cur.GetComponent<ConfigurableJoint>();
			joint.connectedBody = next;
			joint.anchor = Vector3.zero;
			joint.connectedAnchor = next.transform.position - cur.transform.position;
			if (breakable)
				joint.breakForce = breakingForce;
		}

		// final acts really weird
		ConfigurableJoint finalJoint = interRbs[^2].GetComponent<ConfigurableJoint>();
		//finalJoint.anchor = interRbs[^1].transform.position - interRbs[^2].transform.position;
		finalJoint.autoConfigureConnectedAnchor = false;
		finalJoint.connectedAnchor = Vector3.zero;
		
		foreach (Rigidbody rb in interRbs)
		{
			rb.mass = segmentMass;
			rb.drag = segmentDrag;
			rb.GetComponent<SphereCollider>().radius = radius;
		}

		lineRenderer = gameObject.AddComponent<LineRenderer>();
		lineRenderer.startWidth = radius * 2;
		lineRenderer.endWidth = radius * 2;
		lineRenderer.material = cableMaterial;
	}

	void ResetCable()
	{
		if (interRbs == null) return;

		foreach (Rigidbody rb in interRbs)
		{
			Destroy(rb.gameObject);
		}

		Destroy(from.gameObject.GetComponent<ConfigurableJoint>());
	}

	void PreventSpazzing()
	{
		for (int i = 0; i < interRbs.Count; i++)
		{
			Rigidbody rb = interRbs[i];
			if (rb.velocity.sqrMagnitude > maxVelocity * maxVelocity)
			{
				float newDrag = Mathf.Pow(rb.velocity.magnitude - maxVelocity, dragExponent) + segmentDrag;
				if (spazWarning) Debug.LogWarning($"spazzing detected, applying {newDrag} drag");
				rb.drag = newDrag;
			}
			else
			{
				rb.drag = Mathf.Lerp(rb.drag, segmentDrag, returnSpeed);
			}
		}
	}

	/*
	void SetupMesh()
	{
		Mesh mesh = gameObject.AddComponent<MeshFilter>().mesh;
		MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
		renderer.material = cableMaterial;
	}

	void RenderCable()
	{
		mesh.Clear();

		List<Vector3> vertices = new();
		List<int> tris = new();


	}
	*/

	void RenderCable()
	{
		List<Vector3> positions = new();

		foreach (Rigidbody rb in interRbs)
			positions.Add(rb.transform.position);

		List<Vector3> spline = Splines.CatmullRom(positions, resolution);

		lineRenderer.positionCount = spline.Count;
		lineRenderer.SetPositions(spline.ToArray());
	}
}