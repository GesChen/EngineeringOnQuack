using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PUI_Realiser : MonoBehaviour {
	public PUI_Object RealisePanel(PUI_Panel panel) {
		return null;
	}

	public Labels RealiseComponent(PUI_Component component) {
		switch (component) {
			case PUI_Dropdown dropdown: { // goes before button cuz it derives from it

				break;
			}
			case PUI_Button button: {
				break;
			}
			case PUI_Grid grid: {
				break;
			}
			case PUI_List list: {
				break;
			}
			case PUI_Text text: {
				break;
			}
			default:
				throw new("can't use component as itself, must use a derived class");
		}
		return null;
	}
}