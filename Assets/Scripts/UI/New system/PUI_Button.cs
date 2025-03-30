using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PUI_Button : PUI_Component {

	public delegate void ButtonClickEvent();
	public ButtonClickEvent OnClick;

	public PUI_Button(
		ButtonClickEvent onClick,
		Config config)
		: base(config) {
		OnClick = onClick;
	}
}