using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PortVisualizerNumber : MonoBehaviour {
	public TextMeshProUGUI text;
	public Transform target;

	public void SetNumber(int number) {
		text.text = number.ToString();
	}

	void Update() {
		transform.SetPositionAndRotation(target.position, Camera.main.transform.rotation);
	}
}
