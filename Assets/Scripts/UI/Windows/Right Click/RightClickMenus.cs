using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using W = MenuUtil.Window;

public class RightClickMenus : MonoBehaviour {
	public static W testWindow =
		new("testwindow", 200,
			new() {
				new W.Button("digital", "tsetbutton", ()=>Debug.Log("pressed")),
				new W.Button("digital", "123312", ()=>Debug.Log("pressed")),
				new W.Button("digital", "hghfdh", ()=>Debug.Log("pressed")),
				new W.Item("tsetbutton"),
			});
}