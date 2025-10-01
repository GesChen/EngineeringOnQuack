using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleToTarget : MonoBehaviour {
	public RectTransform target;
	RectTransform rt;

	void Start() {
		rt = GetComponent<RectTransform>();
	}

	void LateUpdate() {
		rt.sizeDelta = target.sizeDelta; 
	}
}