using System.Collections.Generic;
using System;
using UnityEngine;

public class PUIPanelHead : MonoBehaviour {
	public Canvas canvas;
	public List<Sprite> icons;
	protected Sprite Icon(string name) => icons.Find(i => i.name == name);

	[HideInNormalInspector] public ProceduralUI main;
}

public class PUIPanel {
	public string PanelTitle;
	public bool AddTitle;
	public Canvas Canvas;
	public List<PUIComponent> Components;
	public ProceduralUI Realised;

	public PUIPanel(
		string panelTitle,
		bool addTitle,
		Canvas canvas,
		List<PUIComponent> components) {

		PanelTitle = panelTitle;
		AddTitle = addTitle;
		Canvas = canvas;
		Components = components;
	}

	public ProceduralUI Realise(GameObject main) {
		ProceduralUI newComponent = main.AddComponent(typeof(ProceduralUI)) as ProceduralUI;

		newComponent.panelTitle = PanelTitle;
		newComponent.addTitle = AddTitle;
		newComponent.canvas = Canvas;
		newComponent.components = Components;

		// recursively realise dropdownmenus
		foreach (PUIComponent puic in newComponent.components) {
			if (puic.Type == PUIComponent.ComponentType.dropdown) {
				ProceduralUI dropdown = puic.DropdownMenuPanel.Realise(main);
				puic.DropdownMenu = dropdown;
			}
		}

		Realised = newComponent;
		return newComponent;
	}
}