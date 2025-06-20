using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomizableMenu : MonoBehaviour {
	public WindowItem title;
	public List<WindowItem> items;
	RectTransform rt;

	void Start() {
		rt = GetComponent<RectTransform>();
	}
	
	public void UpdateActiveState(int[] indices) {
		if (items == null) throw new("Forgot to set items!");

		for (int i = 0; i < items.Count; i++) {
			WindowItem item = items[i];
			item.RealObject.gameObject.SetActive(indices.Contains(i));
		}
	}

	public void UpdateWidth(float width) {
		float height = Config.UI.Menu.ItemHeight;
		Vector2 size = new(width, height);

		if (title != null) title.RealObject.sizeDelta = size;

		foreach (var item in items) {
			item.RealObject.sizeDelta = size;
		}
	}
/*
	/// <summary>
	/// Sets the active state of the items based on a bitmask
	/// </summary>
	/// <param name="bitmask">Expected to have the same number of bits as the length of the items</param>
	public void UpdateActiveState(int bitmask) {
		BitArray bitarray = new(new[] { bitmask });

		// read bits backwards 
		bool[] bits = new bool[bitarray.Length];
		for (int i = 0; i < bitarray.Length; i++) {
			bits[i] = bitarray[bitarray.Length - i - 1];
		}

		// the intermediate step really wasn't needed i just wanted an excuse to make
		// a bool array
		for (int i = 0; i < items.Count; i++) {
			items[i].RealObject.gameObject.SetActive(bits[i]);
		}
	}*/
}