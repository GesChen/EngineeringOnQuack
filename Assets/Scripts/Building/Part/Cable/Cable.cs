using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;

public class Cable : MonoBehaviour
{
	public Transform from;
	public Transform to;
	public int segments;
	public GameObject spanningObject;

	private List<Rigidbody> interRbs;

	void Start()
	{
		SetupCable();
		Debug.Break();
	}

	void SetupCable()
	{
		ResetCable();

		interRbs = new();

		// extra first as child of the start
		GameObject first = Instantiate(spanningObject, from);
		first.transform.localPosition = Vector3.zero;
		Rigidbody firstrb = first.GetComponent<Rigidbody>();
		firstrb.isKinematic = true;
		interRbs.Add(firstrb);

		for (int i = 0; i < segments; i++)
		{
			GameObject inter = Instantiate(spanningObject, transform);
			float phase = (i + 1f) / (segments + 1f);
			Debug.Log(phase);
			inter.transform.position = Vector3.Lerp(from.position, to.position, phase);

			interRbs.Add(inter.GetComponent<Rigidbody>());
		}
		
		for (int i = 0; i < interRbs.Count - 1; i++)
		{
			Rigidbody cur = interRbs[i];
			Rigidbody next = interRbs[i + 1];

			ConfigurableJoint joint = cur.GetComponent<ConfigurableJoint>();
			joint.connectedBody = next;
			joint.anchor = Vector3.zero;
			joint.connectedAnchor = next.transform.position - cur.transform.position;
		}

		// first and last special cases
		Transform end = interRbs[^1].transform;
		ConfigurableJoint endJoint = end.gameObject.AddComponent<ConfigurableJoint>();
		endJoint.connectedBody = to.GetComponent<Rigidbody>();
		endJoint.anchor = Vector3.zero;
		endJoint.connectedAnchor = end.transform.position - to.position;
		
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
