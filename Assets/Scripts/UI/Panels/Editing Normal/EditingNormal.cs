/*using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class EditingNormal : PUIPanelHead
{
	void Awake() {
		PUIPanel digital = new(
			"digital", false, canvas, new() {
				PUIComponent.NewButton("cpu",		"",	null,	Icon("cpu")),
				PUIComponent.NewButton("ram 8kb",	"",	null,	Icon("ram")),
				PUIComponent.NewButton("display",	"",	null,	Icon("display")),
				PUIComponent.NewButton("script",	"",	null,	Icon("script")) 
			});

		PUIPanel mechanical = new(
			"mechanical", false, canvas, new() {
				PUIComponent.NewButton("motor 1",	"",	null,	Icon("motor 1")),
				PUIComponent.NewButton("motor 2",	"",	null,	Icon("motor 2")),
				PUIComponent.NewButton("piston 1",	"",	null,	Icon("piston 1")),
				PUIComponent.NewButton("piston 1",	"",	null,	Icon("piston 2")),
				PUIComponent.NewButton("servo 1",	"",	null,	Icon("servo 1")),
				PUIComponent.NewButton("servo 1",	"",	null,	Icon("servo 2"))
			});

		PUIPanel structural = new(
			"structural", false, canvas, new() {
				PUIComponent.NewButton("cube",		"",	null,	Icon("cube")),
				PUIComponent.NewButton("sphere",	"",	null,	Icon("sphere")),
				PUIComponent.NewButton("cylinder",	"",	null,	Icon("cylinder")),
				PUIComponent.NewButton("wedge",		"",	null,	Icon("wedge"))
			});

		PUIPanel newPart = new(
			"new part", false, canvas, new() {
				PUIComponent.NewDropdown("structural",	"",	structural,	Icon("structural")),
				PUIComponent.NewDropdown("mechanical",	"",	mechanical,	Icon("mechanical")),
				PUIComponent.NewDropdown("digital",		"",	digital,	Icon("digital")),
				PUIComponent.NewButton("cable",			"",	null,		Icon("cable")),
			});

		PUIPanel mainPanel = new(
			"Editing", true, canvas, new() {
				PUIComponent.NewDropdown("new part",	"makes a new part",	newPart, Icon("plus")),
				PUIComponent.NewButton("clean up",	"",	null,	Icon("clean up")),
				PUIComponent.NewButton("undo",		"",	null,	Icon("undo")),
				PUIComponent.NewButton("redo",		"",	null,	Icon("redo"))
			});

		main = mainPanel.Realise(gameObject);
	}
}
*/