using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PortVisualizerNumber : MonoBehaviour {
	public TextMeshProUGUI text;

	public void SetNumber(int number) {
		text.text = number.ToString();
	}

	void Update() {
		transform.rotation = Camera.main.transform.rotation;
	}
}
