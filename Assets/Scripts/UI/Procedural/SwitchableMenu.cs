using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchableMenu : MonoBehaviour {
	public List<WindowItem> items;
	
	public void UpdateActiveState(int[] indices) {
		if (items == null) throw new("Forgot to set items!");

		for (int i = 0; i < items.Count; i++) {
			WindowItem item = items[i];
			item.RealObject.gameObject.SetActive(indices.Contains(i));
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