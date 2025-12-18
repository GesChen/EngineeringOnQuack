using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Creation : MonoBehaviour {
	public Construct Construct;

	public List<Output> Outputs;

	public List<SubAssembled> SubAssemblies;

	public int ID;

	public struct SubAssembled {
		public Transform Parent;
		public List<(int pid, Transform Part)> Parts;
		public Rigidbody RB;
		public float Mass;
		public SubAssemblyParts Source;
	}
	
	public struct SubAssemblyParts {
		public int ID;
		public List<int> Parts; // changed to ids instaed of indexes now
	}
}