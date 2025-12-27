using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Part : MonoBehaviour {
	public int ID;
	public BasePart basePart;
	bool m_Selected = false;
	public bool Selected {
		get => m_Selected;
		set {
			if (m_Selected == value) return;
			m_Selected = value;
			UpdateLayer();
		}
	}

	public PartGroup Group;

	// might turn this into a separate class later but somehow not conflict or confuse with 
	// unnity's material class
	public Color color;
	public Composition composition;

	// tentative, may change this method
	MeshRenderer[] renderers;
	Collider[] colliders;
	[HideInNormalInspector] public Transform[] dontUpdateMaterialsFor;

	[HideInNormalInspector] public Material material;
	[HideInNormalInspector] public PhysicMaterial physicMaterial;

	public bool IsNonStaticPart(out NonStaticPart NSPComponent) {
		NSPComponent = gameObject.GetComponent<NonStaticPart>();

		return NSPComponent != null;
	}
	public NSPType GetNSP<NSPType>() where NSPType : NonStaticPart // i overcomplicated the logic before
		=> gameObject.GetComponent<NSPType>();

	void Start() {
		renderers = GetComponentsInChildren<MeshRenderer>(); // includes self
		colliders = GetComponentsInChildren<Collider>();

		color = Config.Building.Colors[Config.Building.PartDefaultColorIndex];
		composition = Compositions.All[Config.Building.PartDefaultCompositionIndex];
		UpdateMaterial();
	}

	void UpdateLayer() {
		var layer =
			Selected
			? LayerMask.NameToLayer("Selected")
			: LayerMask.NameToLayer("Part");
		
		gameObject.layer = layer;
		foreach (Transform child in transform)
			child.gameObject.layer = layer;
	}

	public void SetColor(Color newCol) { // retains alpha of material
		newCol.a = composition.Material.color.a;
		color = newCol;

		UpdateMaterial();
	}

	public void SetComposition(Composition comp) { // might need extra processing later
		composition = comp;
		color.a = comp.Material.color.a; // fix alpha

		UpdateMaterial();
	}

	public void UpdateMaterial() {
		if (composition == null)
			throw new("Composition is null");

		Material newMat = new(composition.Material);
		
		// treat glass special

		if (composition.Material.color.a > .1f) {
			newMat.color = color;
		} else {
			newMat.SetColor("_TransmittanceColor", color);
		}

		material = newMat;
		foreach (var renderer in renderers) {
			if (dontUpdateMaterialsFor.Contains(renderer.transform)) continue;

			renderer.material = newMat;  // sets first one, idk if i should use the method
		}

		physicMaterial = composition.PhysicsMaterial;
		foreach (var collider in colliders) {
			collider.material = composition.PhysicsMaterial;
		}
	}
}