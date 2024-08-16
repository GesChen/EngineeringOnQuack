using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThrowShit : MonoBehaviour
{
	public GameObject toThrow;
	public float force;
	public float lifetime;

	void Update()
	{
		if (Mouse.current.leftButton.wasPressedThisFrame)
		{
			GameObject newObj = Instantiate(toThrow);
			Rigidbody rb = newObj.GetComponent<Rigidbody>();
			if (rb == null) rb = newObj.AddComponent<Rigidbody>();

			newObj.transform.position = transform.position;
			rb.velocity = transform.forward * force;
			StartCoroutine(killAfterTime(newObj, lifetime));
		}
	}
	IEnumerator killAfterTime(GameObject target, float time)
	{
		yield return new WaitForSeconds(time);
		Destroy(target);
	}
}
