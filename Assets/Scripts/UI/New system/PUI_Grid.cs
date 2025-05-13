using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A fixed sized grid of elements
/// </summary>
public class PUI_Grid : PUI_Component {

	public PUI_Component[,] Components;

	public PUI_Grid(PUI_Component[,] components, Config.Layout layout)
		: base(new(layout: layout)) {
		Components = components;
	}
}