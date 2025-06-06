using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using W = MenuUtil.Window;

public class MaterialEditingMenu : MonoBehaviour {
	// right click version for now
	public static readonly CWindow colorPicker = new(){
		Name = "Color Picker",
		Config = new(){
			Movable = false,
			Resizable = false,
		}
	}

	}
	public static readonly W window = new(
		"Material",
		150,
		new(){
			new W.Flyout()
		});
}