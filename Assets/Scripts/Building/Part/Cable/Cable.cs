using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;

public class Cable : MonoBehaviour
{
	[Header("Config")]
	public Transform from;
	public Transform to;
	public int segments;
	public float segmentRadius;
	public float segmentMass;
	public GameObject spanningObject;

	private List<Rigidbody> interRbs;

	[Space]
	public Transform fromObject;
	public Transform toObject;

	void Start()
	{
		SetupCable();
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

		for (int i = 0; i < segments + 1; i++)
		{
			GameObject inter = Instantiate(spanningObject, transform);
			float phase = (i + 1f) / (segments + 1f);
			Debug.Log(phase);
			inter.transform.position = Vector3.Lerp(from.position, to.position, phase);

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
		}

		// final acts really weird
		ConfigurableJoint finalJoint = interRbs[^2].GetComponent<ConfigurableJoint>();
		//finalJoint.anchor = interRbs[^1].transform.position - interRbs[^2].transform.position;
		finalJoint.autoConfigureConnectedAnchor = false;
		finalJoint.connectedAnchor = Vector3.zero;
		



		foreach (Rigidbody rb in interRbs)
		{
			rb.mass = segmentMass;
			rb.GetComponent<SphereCollider>().radius = segmentRadius;
		}
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
}
