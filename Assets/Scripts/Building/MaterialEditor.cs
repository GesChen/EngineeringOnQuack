using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MaterialEditor : MonoBehaviour {
	public Image ColorPreview;
	public Image MaterialPreview;

	Color? currentColor;
	Composition currentComposition;

	Part[] editingParts;

	public static void SetupComponent(
		CWindow cw, 
		ref RectTransform colorPickerButton, 
		ref RectTransform materialPickerButton) {
		colorPickerButton = cw.Items[0].SubItems[1].RealObject;
		materialPickerButton = cw.Items[0].SubItems[2].RealObject;

		// add materialeditor and set up 
		var editor = cw.RealisedWindow.gameObject.AddComponent<MaterialEditor>();

		int imageSubitemIndex = 0;
		editor.ColorPreview = (Image)
			cw.Items[0].SubItems[1]
			.SubItems[imageSubitemIndex]
			.Construction[0].RealComponent;

		//imageSubitemIndex = 1;
		editor.MaterialPreview = (Image)
			cw.Items[0]
			.SubItems[2]
			.Construction[0].RealComponent;

		// subscribe
		RightClickMenus.OnMaterial += MaterialEditingMenu.ShowMenu;
		RightClickMenus.OnMaterial += (_) => editor.UpdateParts();
		SelectionManager.Instance.OnSelectionChanged += editor.UpdateParts;

		MaterialEditingMenu.OnColorSelection += editor.SetColor;
		MaterialEditingMenu.OnCompositionSelection += editor.SetComposition;
	}

	void Update() {

	}

	public void UpdateParts() {
		var sel = SelectionManager.Instance.selection;

		if (sel.Count == 0)
			UpdateNone();
		else if (sel.Count == 1)
			UpdateSingle(sel[0]);
		else
			UpdateMultiple(sel);

		UpdatePreviews();
	}

	public void UpdateNone() {
		currentColor = Color.white;
		currentComposition = null;
	}

	public void UpdateSingle(Transform t) {
		var part = t.GetComponent<Part>();

		// set the currents to the part's current
		currentColor = part.color;
		currentComposition = part.composition;

		editingParts = new[] { part };
	}

	public void UpdateMultiple(List<Transform> ts) {
		Part[] parts = ts.Select(t => t.GetComponent<Part>()).ToArray();

		bool allSameColor = true;
		bool allSameComposition = true;
		for (int i = 0; i < parts.Length - 1; i++) {
			if (parts[i].color != parts[i + 1].color) {
				allSameColor = false;
			}
			if (parts[i].composition != parts[i + 1].composition) {
				allSameComposition = false;
			}
		}

		currentColor = allSameColor ? parts[0].color : null;
		currentComposition = allSameComposition ? parts[0].composition : null;

		editingParts = parts;
	}

	public void SetColor(Color color) {
		// no null check cuz its getting set lol

		currentColor = color;

		foreach (var part in editingParts) {
			part.SetColor(currentColor.Value);
		}
		UpdatePreviews();
	}

	public void SetComposition(int compIndex) {
		currentComposition = Compositions.All[compIndex];

		foreach (var part in editingParts) {
			part.SetComposition(currentComposition);
		}
		UpdatePreviews();
	}

	void UpdatePreviews() {

		if (currentColor.HasValue) {
			Color preview = currentColor.Value;
			preview.a = 1f;
			ColorPreview.color = preview;
			ColorPreview.sprite = null;
		} else {
			ColorPreview.color = Config.UI.Visual.TextColor;
			ColorPreview.sprite = Config.Building.ColorIcon;
		}

		MaterialPreview.sprite =
			currentComposition != null
			? currentComposition.Icon 
			: Config.Building.MaterialIcon;
	}
}