using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MaterialEditor : MonoBehaviour {
	public Image ColorPreview;
	public Image MaterialPreview;

	Color currentColor;
	Materials.IMaterial currentMaterial;

	public static void SetupComponent(
		CWindow cw, 
		ref RectTransform colorPickerButton, 
		ref RectTransform materialPickerButton) {
		colorPickerButton = cw.Items[0].SubItems[1].RealObject;
		materialPickerButton = cw.Items[0].SubItems[2].RealObject;

		// add materialeditor and set up 
		var editor = cw.RealisedWindow.gameObject.AddComponent<MaterialEditor>();

		int imageSubitemIndex = 1;
		editor.ColorPreview = (UnityEngine.UI.Image)
			cw.Items[0].SubItems[1].SubItems[imageSubitemIndex]
			.Construction[0].RealComponent;

		//imageSubitemIndex = 1;
		editor.MaterialPreview = (UnityEngine.UI.Image)
			cw.Items[0].SubItems[2]
			.Construction[0].RealComponent;

		// subscribe
		RightClickMenus.OnMaterial += MaterialEditingMenu.ShowMenu;
	}

	void Update() {

	}

	public void SetColor(Color color) {
		currentColor = color;
		UpdatePreviews();
	}

	public void SetMaterial(Materials.IMaterial material) {
		currentMaterial = material;
		UpdatePreviews();
	}

	void UpdatePreviews() {
		ColorPreview.color = currentColor;
	}
}