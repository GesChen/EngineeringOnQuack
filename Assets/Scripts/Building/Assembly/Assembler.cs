using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using UnityEngine;

public class Assembler : Singleton<Assembler> {
	// rewritten assembler with basically the same methods as before but
	// just better because the old code sucks so much wtf

	public struct Connection {
		public int A;
		public int B;
	}
	public struct SubAssembly {
		public int ID;
		public List<int> Parts;
	}
	public struct Assembled {
		public Transform Parent;
		public List<(int pi, Transform Part)> Parts;
		public Rigidbody RB;
		public float Mass;
		public SubAssembly Source;
	}
	public struct AxleConnection {
		public int AxleAssembly;
		public int ConnectedAssemblyIndex;
		public Vector3 JointPos;
		public Vector3 axis;
	}

	public void Assemble(out List<Assembled> assembleds) {
		var parts = BuildingManager.Instance.Assembly.Parts;

		// lmao WTF
		//SetupPhysics(CopyToSimulation(ConnectionsToSubAssemblies(FindAllConnections())));

		var connections = FindAllConnections(parts);
		var subassemblies = ConnectionsToSubAssemblies(connections, parts);
		var assembled = CopyToSimulation(subassemblies, parts);
		SetupPhysics(assembled, parts);

		assembleds = assembled;
	}

	// wrote this way if perhaps in the future cables or other
	// needs special checking, been abstracted so can modify this
	// method to account for that
	bool PartIsAxle(Part part) {
		return part.GetComponent<Part_Axle>() != null; // also this check will be changed later 
	}

	public List<Connection> FindAllConnections(List<Part> parts) {
		List<Connection> connections = new();

		for (int a = 0; a < parts.Count; a++) {
			for (int b = a + 1; b < parts.Count; b++) {
				if (TestTwoPartConnection(
					parts[a],
					parts[b]
					))
					connections.Add(
						new() {
							A = a,
							B = b,
						});
			}
		}

		return connections;
	}

	// method does extra check for axles in the following manner: ---------
	// if both parts are normal, just check for intersect
	// if one is axle, perform normal axle check
	// if both are axles, do intersection check i guess? like normal both parts
	bool TestTwoPartConnection(Part A, Part B) {
		bool aIsAxle = PartIsAxle(A);
		bool bIsAxle = PartIsAxle(B);

		if (aIsAxle == bIsAxle) { // both are either part or axle
								  // so do normal meshes

			Vector3[] AWSVerts = PartUtil.WorldSpaceVertsOfPart(A);
			Vector3[] BWSVerts = PartUtil.WorldSpaceVertsOfPart(B);
			int[] Atris = A.basePart.AllTris;
			int[] Btris = B.basePart.AllTris;

			return Intersections.MeshesIntersectRawMesh(AWSVerts, BWSVerts, Atris, Btris);

		} else { // one is axle 
			Part axlePart = aIsAxle ? A : B;
			Part normPart = aIsAxle ? B : A;

			// only connect if either end of axle is inside the normal
			Part_Axle axle = axlePart.GetComponent<Part_Axle>();
			Triangle[] partTris = PartUtil.PartToWSTriList(normPart);

			Vector3 pointA = axle.endA.position;
			Vector3 pointB = axle.endB.position;

			if (Intersections.PointInMesh(pointA, partTris)) return true;
			if (Intersections.PointInMesh(pointB, partTris)) return true;
			return false;
		}
	}

	public List<SubAssembly> ConnectionsToSubAssemblies(List<Connection> connections, List<Part> parts) {
		// i COULD use the old method
		// i was gonna rewrite this but actually fuck nah im lazy
		// we porting bruh fts

		int subI = 0;

		Dictionary<int, bool> partsInAssemblies = 
			Enumerable.Range(0, parts.Count).ToDictionary(part => part, value => false);
		List<SubAssembly> assemblies = new();

		foreach (var connection in connections) {
			int A = connection.A;
			int B = connection.B;

			partsInAssemblies[A] = true;
			partsInAssemblies[B] = true;

			// if no assembly contains part a or b
			bool containsA = assemblies.Any(a => a.Parts.Contains(A));
			bool containsB = assemblies.Any(a => a.Parts.Contains(B));
			if (!(containsA || containsB)) {
				SubAssembly newAssembly = new() {
					Parts = new() { A, B },
					ID = subI++
				};

				assemblies.Add(newAssembly);
			} else {
				int assemblyIndex = -1;
				if (containsA) assemblyIndex = assemblies.FindIndex(a => a.Parts.Contains(A));
				if (containsB) assemblyIndex = assemblies.FindIndex(a => a.Parts.Contains(B)); // could be optimized but im lazy + it looks better

				if (!containsA) assemblies[assemblyIndex].Parts.Add(A);
				if (!containsB) assemblies[assemblyIndex].Parts.Add(B);
			}
		}

		List<int> partsLeft = partsInAssemblies.Where(kvp => kvp.Value == false).Select(kvp => kvp.Key).ToList();
		foreach (int part in partsLeft) {
			SubAssembly sub = new() {
				Parts = new() { part },
				ID = subI++
			};

			assemblies.Add(sub); // solo parts become own assembly
		}

		// number subassemblies

		return assemblies;
	}

	// can we rewrite this?????
	List<Assembled> CopyToSimulation(List<SubAssembly> subassemblies, List<Part> Parts) {
		// also a straight port

		List<Assembled> assembleds = new();
		foreach (SubAssembly sub in subassemblies) {

			Transform subParent = new GameObject($"SubAssembly ({sub.Parts.Count})").transform;
			subParent.parent = BuildingManager.Instance.SimulationContainer;

			List<Transform> parts = new();
			Vector3 accumPos = Vector3.zero;

			foreach (int partIndex in sub.Parts) {
				Part part = Parts[partIndex];

				Transform newObject = Instantiate(part.gameObject).transform;

				newObject.gameObject.SetActive(true);
				var partComp = newObject.GetComponent<Part>();
				partComp.enabled = false;

				//????
				BuildingManager.Instance.Assembly.Parts.Remove(partComp);

				parts.Add(newObject);

				accumPos += newObject.transform.position;
			}

			subParent.position = accumPos / sub.Parts.Count;
			foreach (Transform part in parts)
				part.parent = subParent;

			// finalize all
			foreach (Transform newPart in parts)
				if (newPart.TryGetComponent<NonStaticPart>(out var origNSP))
					origNSP.FinalizeInstantiation(newPart.gameObject);

			assembleds.Add(new() {
				Parent = subParent,
				Parts = sub.Parts.Zip(parts, (pi, part) => (pi, part)).ToList(),
				Source = sub
			});
		}

		return assembleds;
	}

	void SetupPhysics(List<Assembled> assembleds, List<Part> parts) {
		AddRBs(assembleds);

		CalculateAssemblyMasses(assembleds, parts);

		var joints = CalculateAxleJoints(assembleds, parts);

		ApplyAxleConnections(joints, assembleds);
	}

	void CalculateAssemblyMasses(List<Assembled> assembleds, List<Part> parts) {
		for (int i = 0; i < assembleds.Count; i++) {
			Assembled assembled = assembleds[i];

			assembled.Mass = SubassemblyTotalMass(assembleds[i].Source, parts);

			assembleds[i] = assembled;
		}
	}

	void AddRBs(List<Assembled> assembleds) {
		for (int i = 0; i < assembleds.Count; i++) {
			Assembled assembled = assembleds[i];

			var rb = assembled.Parent.gameObject.AddComponent<Rigidbody>();
			assembled.RB = rb;
			rb.mass = assembled.Mass;
			
			assembleds[i] = assembled;
		}
	}

	float SubassemblyTotalMass(SubAssembly asm, List<Part> parts) {
		float total = 0;
		foreach (var pi in asm.Parts) {
			var part = parts[pi];

			total += CalculatePartMass(part);
		}
		return total;
	}

	float CalculatePartMass(Part part) {
		float total = 0;
		// iterate through tris
		for (int i = 0; i < part.basePart.AllTriPositions.Length; i += 3) {
			Vector3 p1 = part.transform.TransformPoint(part.basePart.AllTriPositions[i + 0]);
			Vector3 p2 = part.transform.TransformPoint(part.basePart.AllTriPositions[i + 1]);
			Vector3 p3 = part.transform.TransformPoint(part.basePart.AllTriPositions[i + 2]);

			total += Vector3.Dot(p1, Vector3.Cross(p2, p3)) / 6f;
		}

		return total;
	}

	public List<AxleConnection> CalculateAxleJoints(List<Assembled> assembleds, List<Part> parts) {
		List<int> axleParts = new();
		foreach (var assembly in assembleds)
			axleParts.AddRange(
				assembly.Source.Parts.Where(pi => PartIsAxle(parts[pi])));

		List<AxleConnection> connections = new();

		foreach (int api in axleParts) {
			int assemblyofpart = assembleds
				.First(a => a.Source.Parts.Contains(api)).Source.ID;

			Part_Axle axle = parts[api].GetComponent<Part_Axle>();

			for (int connectionI = 0; connectionI < assembleds.Count; connectionI++) {
				Assembled assembled = assembleds[connectionI];
				var subAssembly = assembled.Source;

				if (subAssembly.ID == assemblyofpart) continue; // dont check itself

				if (AxleCalculationHelper.AxleIntersectionTest(
					subAssembly,
					axle.endA.position,
					axle.endB.position,
					out Vector3 jointPos
					)) {
					// add joint on axle connecting it to the sub's parent

					connections.Add(new() {
						AxleAssembly = assemblyofpart,
						ConnectedAssemblyIndex = connectionI,
						JointPos = jointPos,
						axis = (axle.endB.position - axle.endA.position).normalized
					});
				}
			}
		}

		return connections;
	}

	void ApplyAxleConnections(List<AxleConnection> axleConnections, List<Assembled> assembleds) {
		foreach (var ac in axleConnections) {
			int assembly = ac.AxleAssembly;
			var parentsub = assembleds[assembly].Parent;
			var joint = parentsub.gameObject.AddComponent<HingeJoint>();

			int connectedIndex = ac.ConnectedAssemblyIndex;
			joint.connectedBody = assembleds[connectedIndex].RB;

			joint.anchor = parentsub.InverseTransformPoint(ac.JointPos);

			joint.axis = ac.axis;
		}
	}
}