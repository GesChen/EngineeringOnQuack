using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class AutoFocus : MonoBehaviour
{
	public Volume Volume;
	public float smoothness;

	DepthOfField dof;
	float target;
	float current;

	void Start()
	{
		Volume.profile.TryGet(out dof);
		current = dof.focusDistance.value;
	}

	void Update()
	{
		Physics.Raycast(Camera.main.ScreenPointToRay(new(Screen.width / 2, Screen.height / 2)), out RaycastHit hit);
		target = hit.distance;
		current = Mathf.Lerp(current, target, smoothness);

		dof.focusDistance.value = current;
	}
}
