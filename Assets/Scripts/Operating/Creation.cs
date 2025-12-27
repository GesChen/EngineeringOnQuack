using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Creation : MonoBehaviour {
	public Construct Construct;

	public List<Output> Outputs;

	public List<SubAssembly> SubAssemblies;

	public int ID;

	public class SubAssembly {
		public int ID;
		public Transform Transform;
		public List<Part> Parts;
		public Rigidbody RB;
		public float Mass;
	}

	// only an approximation cuz it doesnt actually use the volume based COM 
	public Vector3 GetCenterOfMassApprox() {
		float totalMass = SubAssemblies.Sum(sa => sa.Mass);

		Vector3 positionMassSum = Vector3.zero;
		foreach (var sa in SubAssemblies) {
			positionMassSum += sa.Transform.position * sa.Mass;
		}

		return positionMassSum / totalMass;
	}

	// cant just save the construct bc moving parts n shi
	// plus destruction later
	// but gotta keep construct for editing n whatever
	public struct Serializable {
		public int ID;
		public Construct Construct;

		// dont need to save outputs those come from the construct n cant change

		public TransformData Transform;

		public SubAssembly[] SubAssemblies;

		public struct SubAssembly {
			public int ID;
			public Part[] Parts; // has all the states we need why not use it
			public float Mass;

			public TransformData Transform;

			public struct Part {
				public TransformData Transform; // use this world position/rotation instead

				public Construct.Part source;

				public static explicit operator Part(global::Part part) => new() {
					Transform = (TransformData)part.transform,
					source = (Construct.Part)part
				};
			}

			public static explicit operator SubAssembly(Creation.SubAssembly sub) => new() {
				ID = sub.ID,
				Parts = sub.Parts.Select(p => (Part)p).ToArray(),
				Mass = sub.Mass,
				Transform = (TransformData)sub.Transform
			};
		}
	}

	public Serializable ConvertToSerializable() => new() {
		ID = ID,
		Construct = Construct,
		Transform = (TransformData)transform,
		SubAssemblies = SubAssemblies.Select(sa => (Serializable.SubAssembly)sa).ToArray()
	};
}