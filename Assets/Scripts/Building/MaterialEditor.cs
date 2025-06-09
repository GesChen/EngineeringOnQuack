using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MaterialEditor : MonoBehaviour {
	public Image ColorPreview;
	public Image MaterialPreview;

	Color currentColor;
	Materials.Material currentMaterial;

	void Update() {

	}

	public void SetColor(Color color) {
		currentColor = color;
		UpdatePreviews();
	}

	public void SetMaterial(Materials.Material material) {
		currentMaterial = material;
		UpdatePreviews();
	}

	void UpdatePreviews() {
		ColorPreview.color = currentColor;
	}
}