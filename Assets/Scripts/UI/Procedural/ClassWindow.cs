using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassWindow {
	public string Name;
	public WindowItem[] Items;

	public class Config {
		public bool Resizable = true;
		public bool Movable = true;
		public Color Color = global::Config.UI.Visual.BackgroundColor;
	}

	public Config Configuration = new();
}