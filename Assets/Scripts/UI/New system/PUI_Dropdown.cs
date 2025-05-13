using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PUI_Dropdown : PUI_Button {
	// realised objects have to hook onto onbuttonclick for dropping down

	public enum Types {
		DownwardList,
		Outward
	}

	public Types Type;

	public PUI_List DownwardContents;
	public PUI_Panel OutwardPanel;

	public PUI_Dropdown(
		Types type,
		Config config,
		PUI_List downwardContents = null,
		PUI_Panel outwardPanel = null)

		: base(null, config) {

		Type = type;
		DownwardContents = downwardContents;
		OutwardPanel = outwardPanel;
	}

	public static PUI_Dropdown NewOutward(
		List<PUI_Component> components,
		Config config){

		PUI_Panel newPanel = new(components, PUI_Panel.Config.NoInteraction);
		return new(Types.Outward, config, null, newPanel);
	}

	public static PUI_Dropdown NewDownward(
		PUI_Component[] components,
		Config config,
		Config.Layout componentsLayout) {

		PUI_List newList = new(
			components, 
			PUI_List.Orientations.Vertical, 
			new (layout: componentsLayout));
		return new(Types.DownwardList, config, newList, null);
	}
}