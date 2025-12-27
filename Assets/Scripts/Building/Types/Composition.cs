// temporary i guess idk where else to put this
//public class Materials {
// renaming may happen later
using UnityEngine;
using System.Linq;

public class Composition {
	public int ID;
	public string Name;

	public readonly string IconLocation;
	private readonly string MaterialLocation;
	private readonly string PhysicsLocation;

	protected Sprite m_Icon;
	public Sprite Icon => HF.LoadResource(ref m_Icon, IconLocation);

	protected Material m_Material;
	public Material Material => HF.LoadResource(ref m_Material, MaterialLocation);

	protected PhysicMaterial m_PhysicsMaterial;
	public PhysicMaterial PhysicsMaterial => HF.LoadResource(ref m_PhysicsMaterial, PhysicsLocation);

	public Composition(
		int id,
		string name,
		string iconLocation,
		string materialLocation,
		string physicsLocation) {

		ID = id;
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
		0,
		"Wood",
		Config.Locations.IconsFolder + "Composition/wood1",
		Config.Locations.MaterialsFolder + "Wood",
		Config.Locations.MaterialsFolder + "Physics/Wood"
	);

	public static readonly Composition Concrete = new(
		1,
		"Concrete",
		Config.Locations.IconsFolder + "Composition/concrete1",
		Config.Locations.MaterialsFolder + "Concrete",
		Config.Locations.MaterialsFolder + "Physics/Concrete"
	);

	public static readonly Composition Metal = new(
		2,
		"Metal",
		Config.Locations.IconsFolder + "Composition/metal4",
		Config.Locations.MaterialsFolder + "Metal",
		Config.Locations.MaterialsFolder + "Physics/Metal"
	);

	public static readonly Composition Glass = new(
		3,
		"Glass",
		Config.Locations.IconsFolder + "Composition/glass1",
		Config.Locations.MaterialsFolder + "Glass",
		Config.Locations.MaterialsFolder + "Physics/Glass"
	);

	public static readonly Composition[] All = {
		Wood,
		Concrete,
		Metal,
		Glass
	};

	public static Composition Get(int id) =>
		All.FirstOrDefault(c => c.ID == id);
}