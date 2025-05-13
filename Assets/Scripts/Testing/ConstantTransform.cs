using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantTransform : MonoBehaviour
{
	public Vector3 translate;
	public Vector3 rotate;

	void Update()
	{
		transform.Translate(translate * Time.deltaTime);
		transform.Rotate(rotate * Time.deltaTime);
	}
}
