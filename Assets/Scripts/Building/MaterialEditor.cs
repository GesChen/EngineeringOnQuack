using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal.VR;
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
		colorPickerButton = cw.Items[0].SubItems[1].RealObject();
		materialPickerButton = cw.Items[0].SubItems[2].RealObject();

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

		Subscribe(editor);
	}

	static void Subscribe(MaterialEditor editor) {
		RightClickMenus.OnMaterialOpened				= editor.ShowMaterialMenu;
		SelectionManager.Instance.OnSelectionChanged	= editor.UpdateParts;
		MaterialEditingMenu.OnColorSelection			= editor.SetColor;
		MaterialEditingMenu.OnCompositionSelection		= editor.SetComposition;
		BottomBar.OnMaterialOpened						= editor.ShowMenuCenter;
	}

	void ShowMaterialMenu(WindowItem source) {
		UpdateParts();
		MaterialEditingMenu.ShowMenu(source);
	}

	void ShowMenuCenter() {
		UpdateParts();
		MaterialEditingMenu.ShowMenu();
	}

	public void UpdateParts() {
		var sel = SelectionManager.Instance.PartSelection;

		if (sel.Length == 0)
			UpdateNone();
		else if (sel.Length == 1)
			UpdateSingle(sel[0]);
		else
			UpdateMultiple(sel);

		UpdatePreviews();
	}

	public void UpdateNone() {
		currentColor = null;
		currentComposition = null;

		editingParts = new Part[0];
	}

	public void UpdateSingle(Part t) {
		var part = t.GetComponent<Part>();

		// set the currents to the part's current
		currentColor = part.color;
		currentComposition = part.composition;

		editingParts = new[] { part };
	}

	public void UpdateMultiple(Part[] parts) {
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