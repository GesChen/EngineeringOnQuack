using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Creation : MonoBehaviour {
	public Construct Construct;

	public List<Output> Outputs;

	public List<SubAssembly> SubAssemblies;

	public int ID;

	public struct SubAssembly {
		public int ID;
		public Transform Parent;
		public List<Part> Parts;
		public Rigidbody RB;
		public float Mass;
	}
}