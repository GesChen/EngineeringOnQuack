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

	public struct Serializable {
		public Vector3 position;
		public Quaternion rotation;

		public struct SubAssembly {

		}
	}
}