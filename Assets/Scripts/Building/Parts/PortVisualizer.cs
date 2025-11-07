using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PortVisualizer : Singleton<PortVisualizer> {

	public Canvas WSCanvas;
	public float NumSize;
	public GameObject NumberPrefab;

	public bool Visualizing;
	bool LastVisualizing = false;

	private List<Transform> Numbers = new();

	void Update() {
		CheckVisualizing();

		if (Visualizing != LastVisualizing) {
			if (Visualizing) CreateNumbers();
			else DestroyAllNumbers();
		}
		LastVisualizing = Visualizing;
	}

	void CheckVisualizing() {
		// criterion: cc or nsp selected
		foreach (var p in SelectionManager.Instance.PartSelection) {
			if (p.IsNonStaticPart(out var nsp)) {
				// criterion subject to change
				if (nsp is Part_CPU or Part_CableConnection) { // first time using or lmao
					Visualizing = true;
					return;
				}
			}
		}
		Visualizing = false;
	}

	void CreateNumbers() {
		// find all ports in assembly
		List<Port> allPorts = new();
		foreach (var p in BuildingManager.Instance.Assembly.Parts) {
			if (p.IsNonStaticPart(out var nsp)) {
				allPorts.AddRange(nsp.Ports);
			}
		}

		foreach (var p in allPorts) {
			// determine port i
			int pi = Array.IndexOf(p.MainNSP.Ports, p);
			var num = CreateNumber(pi, p.transform);

			Numbers.Add(num);
		}
	}

	Transform CreateNumber(int num, Transform target) {
		var obj = Instantiate(NumberPrefab, WSCanvas.transform);

		var visComp = obj.GetComponent<PortVisualizerNumber>();
		visComp.SetNumber(num);
		visComp.target = target;

		obj.GetComponent<RectTransform>().sizeDelta = NumSize * Vector2.one;

		return obj.transform;
	}

	void DestroyAllNumbers() {
		foreach (var num in Numbers) {
			Destroy(num.gameObject);
		}

		Numbers.Clear();
	}
}