using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Assembly {
	public string name;
	public List<PartInfo> parts;
	public bool didPrecomputations;
	public List<SerializableSubassembly> precomputedSubassemblies;
	// to add onto
}

// temporary i guess idk where else to put this
//public class Materials {
// renaming may happen later
public class Composition {
	public string Name;

	public readonly string IconLocation;
	private readonly string MaterialLocation;
	private readonly string PhysicsLocation;

	protected Sprite m_Icon;
	public Sprite Icon						=> HF.LoadCached(ref m_Icon, IconLocation);

	protected Material m_Material;
	public Material Material				=> HF.LoadCached(ref m_Material, MaterialLocation);

	protected PhysicMaterial m_PhysicsMaterial;
	public PhysicMaterial PhysicsMaterial	=> HF.LoadCached(ref m_PhysicsMaterial, PhysicsLocation);

	public Composition(
		string name,
		string iconLocation,
		string materialLocation,
		string physicsLocation) {

		Name = name;
		IconLocation = iconLocation;
		MaterialLocation = materialLocation;
		PhysicsLocation = physicsLocation;
	}
}
//wood
//concrete
//metal
//glass

public static class Compositions {
	public static readonly Composition Wood = new(
		"Wood",
		Config.UI.Locations.IconsFolder + "Composition/wood1",
		"",
		""
	);

	public static readonly Composition Concrete = new(
		"Concrete",
		Config.UI.Locations.IconsFolder + "Composition/concrete1",
		"",
		""
	);

	public static readonly Composition Metal = new(
		"Metal",
		Config.UI.Locations.IconsFolder + "Composition/metal4",
		"",
		""
	);

	public static readonly Composition Glass = new(
		"Glass",
		Config.UI.Locations.IconsFolder + "Composition/glass1",
		"",
		""
	);
}