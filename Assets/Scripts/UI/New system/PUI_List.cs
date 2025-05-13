using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vertical or horizontal arrangement of Components
/// </summary>
public class PUI_List : PUI_Component {
	public enum Orientations {
		Vertical,
		Horizontal
	}

	public PUI_Component[] Components;
	public Orientations Orientation;

	/// <summary>
	/// Constructor for a new List
	/// </summary>
	/// <param name="components">Array of Components for the List</param>
	/// <param name="orientation">If the List renders left to right or top to bottom</param>
	/// <param name="config">Config Parameter should be used with new Config NoName</param>
	public PUI_List(PUI_Component[] components, Orientations orientation, Config config)
		: base(config) {

		Components = components;
		Orientation = orientation;
	}
}