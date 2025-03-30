using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PUI_Panel {
	public List<PUI_Component> Components;

	public class Config {
		public bool Draggable;
		public bool Resizable;
		public bool ShowTitle; // overrides showclosebutton if disabled
		public bool ShowClose;

		public Config(bool draggable, bool resizable, bool showTitle, bool showClose) {
			Draggable = draggable;
			Resizable = resizable;
			ShowTitle = showTitle;
			ShowClose = showClose;
		}

		public static Config NoInteraction = new (false, false, false, false);
	}

	public Config Configuration;

	public PUI_Panel(
		List<PUI_Component> components, 
		Config config) {
		Components = components;
		Configuration = config;
	}
}