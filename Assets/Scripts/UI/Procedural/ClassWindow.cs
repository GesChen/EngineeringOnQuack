using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassWindow {
	public WindowItem[] Items;

	public struct Config {
		public bool Resizable;
		public bool Movable;
	}

	public Config Configuration;
}