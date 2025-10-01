using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part : MonoBehaviour {
	public int ID;
	public BasePart basePart;
	public bool Selected;

	public PartGroup Group;

	// might turn this into a separate class later but somehow not conflict or confuse with 
	// unnity's material class
	public Color color;
	public Composition composition;

	// tentative, may change this method
	MeshRenderer[] renderers;
	Collider[] colliders;

	[HideInNormalInspector] public Material material;
	[HideInNormalInspector] public PhysicMaterial physicMaterial;

	public bool IsNonStaticPart(out NonStaticPart NSPComponent) {
		NSPComponent = gameObject.GetComponent<NonStaticPart>();

		return NSPComponent != null;
	}

	void Start() {
		renderers = GetComponentsInChildren<MeshRenderer>(); // includes self
		colliders = GetComponentsInChildren<Collider>();

		color = Config.Building.Colors[Config.Building.PartDefaultColorIndex];
		composition = Compositions.All[Config.Building.PartDefaultCompositionIndex];
		UpdateMaterial();
	}

	void Update() {
		if (Selected) {
			gameObject.layer = LayerMask.NameToLayer("Selected");
		} else {
			gameObject.layer = LayerMask.NameToLayer("Part");
		}
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
			renderer.material = newMat;  // sets first one, idk if i should use the method
		}

		physicMaterial = composition.PhysicsMaterial;
		foreach (var collider in colliders) {
			collider.material = composition.PhysicsMaterial;
		}
	}
}