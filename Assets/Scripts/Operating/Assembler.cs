using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using UnityEngine;
using UnityEngine.UIElements;

public class Assembler : Singleton<Assembler> {
	// rewritten assembler with basically the same methods as before but
	// just better because the old code sucks so much wtf

	public struct Connection {
		public int A;
		public int B;
	}
	public struct AxleConnection {
		public int AxleAssembly;
		public int ConnectedAssemblyIndex;
		public Vector3 JointPos;
		public Vector3 axis;
	}

	/// <summary>
	/// the main method
	/// </summary>
	public void Assemble(Construct construct, out Creation creation) {
		var parts = construct.Parts;

		// lmao WTF
		//SetupPhysics(CopyToSimulation(ConnectionsToSubAssemblies(FindAllConnections())));
		
		creation = CreateCreation(construct);

		var connections = FindAllConnections(parts);
		var subassemblies = ConnectionsToSubAssemblies(connections, parts);
		var assembledSubs = CopyToSimulation(creation, subassemblies, parts);
		SetupPhysics(assembledSubs, parts);

		SetupCreation(creation, construct, assembledSubs);

		FinalizeNSPSetup(assembledSubs, creation);
	}

	// wrote this way if perhaps in the future cables or other
	// needs special checking, been abstracted so can modify this
	// method to account for that
	bool PartIsAxle(Construct.Part part) =>
		part is Part_Axle.CPart; // also this check will be changed later 

	List<Connection> FindAllConnections(List<Construct.Part> parts) {
		List<Connection> connections = new();

		for (int a = 0; a < parts.Count; a++) {
			for (int b = a + 1; b < parts.Count; b++) {
				if (TestTwoPartConnection(
					parts[a],
					parts[b]))
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
	bool TestTwoPartConnection(Construct.Part A, Construct.Part B) {
		bool aIsAxle = PartIsAxle(A);
		bool bIsAxle = PartIsAxle(B);

		static Vector3[] WSVerts(Construct.Part part) {
			Vector3[] verts = part.GetBasePart().AllVerts;
			part.TransformPoints(verts);
			return verts;
		}

		if (aIsAxle == bIsAxle) { // both are either part or axle
								  // so do normal meshes
			Vector3[] AWSVerts = WSVerts(A);
			Vector3[] BWSVerts = WSVerts(B);
			int[] Atris = A.GetBasePart().AllTris;
			int[] Btris = B.GetBasePart().AllTris;

			return Intersections.MeshesIntersectRawMesh(AWSVerts, BWSVerts, Atris, Btris);

		} else { // one is axle since they are different
			Construct.Part axlePart = aIsAxle ? A : B;
			Construct.Part nonPart = aIsAxle ? B : A;

			// only connect if either end of axle is inside the normal
			var axle = axlePart as Part_Axle.CPart;

			Vector3[] nonVerts = WSVerts(nonPart);
			Triangle[] partTris = Triangle.FromVertexArray(
				nonVerts,
				nonPart.GetBasePart().AllTris);

			Vector3 pointA = axle.endAPos;
			Vector3 pointB = axle.endBPos;

			if (Intersections.PointInMesh(pointA, partTris)) return true;
			if (Intersections.PointInMesh(pointB, partTris)) return true;
			return false;
		}
	}

	List<Creation.SubAssemblyParts> ConnectionsToSubAssemblies(List<Connection> connections, List<Construct.Part> parts) {
		// i COULD use the old method
		// i was gonna rewrite this but actually fuck nah im lazy
		// we porting bruh fts

		int subI = 0;

		Dictionary<int, bool> partsInAssemblies = 
			Enumerable.Range(0, parts.Count).ToDictionary(part => part, value => false);
		List<Creation.SubAssemblyParts> assemblies = new();

		foreach (var connection in connections) {
			int A = connection.A;
			int B = connection.B;

			partsInAssemblies[A] = true;
			partsInAssemblies[B] = true;

			// if no assembly contains part a or b
			bool containsA = assemblies.Any(a => a.Parts.Contains(A));
			bool containsB = assemblies.Any(a => a.Parts.Contains(B));
			if (!(containsA || containsB)) {
				Creation.SubAssemblyParts newAssembly = new() {
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
			Creation.SubAssemblyParts sub = new() {
				Parts = new() { part },
				ID = subI++
			};

			assemblies.Add(sub); // solo parts become own assembly
		}

		// number subassemblies

		return assemblies;
	}

	// can we rewrite this?????
	List<Creation.SubAssembled> CopyToSimulation(Creation creation, List<Creation.SubAssemblyParts> subassemblies, List<Construct.Part> Parts) {
		// also a straight port

		List<Creation.SubAssembled> assembleds = new();
		foreach (Creation.SubAssemblyParts sub in subassemblies) {
			Transform subParent = new GameObject($"SubAssembly ({sub.Parts.Count})").transform;
			subParent.SetParent(creation.transform);

			List<Transform> parts = new();
			Vector3 accumPos = Vector3.zero;

			Dictionary<Construct.Part, Transform> partMap = new();
			foreach (int partIndex in sub.Parts) {
				Construct.Part part = Parts[partIndex];

				Transform newObject = Instantiate(part.GetBasePart().Prefab).transform;
				newObject.SetLocalPositionAndRotation(part.position, part.rotation);
				newObject.localScale = part.scale;

				partMap[part] = newObject;

				newObject.gameObject.SetActive(true);
				var partComp = newObject.GetComponent<Part>();
				partComp.enabled = false;
				partComp.ID = part.id;

				//????
				BuildingManager.Instance.Assembly.Parts.Remove(partComp);

				parts.Add(newObject);

				accumPos += newObject.transform.position;
			}

			// doesnt matter where it is but might as well
			subParent.position = accumPos / sub.Parts.Count;
			foreach (Transform part in parts)
				part.parent = subParent;

			// finalize all
			foreach (int origPartI in sub.Parts) {
				var origPart = Parts[origPartI];

				var newPart = partMap[origPart];
				origPart.FinalizeInstantiation(newPart.gameObject, creation.gameObject);
			}

			assembleds.Add(new() {
				Parent = subParent,
				Parts = sub.Parts.Zip(parts, (pi, part) => (pi, part)).ToList(),
				Source = sub
			});
		}

		return assembleds;
	}

	void SetupPhysics(List<Creation.SubAssembled> assembleds, List<Construct.Part> parts) {
		AddRBs(assembleds);

		CalculateAssemblyMasses(assembleds, parts);

		var joints = CalculateAxleJoints(assembleds, parts);

		ApplyAxleConnections(joints, assembleds);
	}

	Creation CreateCreation(Construct construct) {
		GameObject newObj = new(construct.Name);
		newObj.transform.SetParent(GameManager.Instance.CreationsContainer);
		return newObj.AddComponent<Creation>();
	}

	void SetupCreation(Creation creation, Construct construct, List<Creation.SubAssembled> assembleds) {
		creation.SubAssemblies = assembleds;
		creation.Construct = construct;
		creation.Outputs = construct.Outputs.Select(o =>
			new Output() {
				Name = o
			}).ToList();
		creation.ID = HF.UIDHashFunction();
	}

	void FinalizeNSPSetup(List<Creation.SubAssembled> subs, Creation creation) {
		// set all nsp creationids
		foreach (var sub in subs)
			foreach (var part in sub.Parts)
				if (part.Part.TryGetComponent(typeof(NonStaticPart), out var nsp)) {
					var nspComp = ((NonStaticPart)nsp);
					nspComp.CreationID = creation.ID;
					nspComp.BecomeAssembled();
				}
	}

	// --------- helper functs-----------------
	void CalculateAssemblyMasses(List<Creation.SubAssembled> assembleds, List<Construct.Part> parts) {
		for (int i = 0; i < assembleds.Count; i++) {
			Creation.SubAssembled assembled = assembleds[i];

			assembled.Mass = SubassemblyTotalMass(assembleds[i].Source, parts);

			assembleds[i] = assembled;
		}
	}

	void AddRBs(List<Creation.SubAssembled> assembleds) {
		for (int i = 0; i < assembleds.Count; i++) {
			Creation.SubAssembled assembled = assembleds[i];

			assembled.RB = HF.GetOrMakeRigidBody(assembled.Parent.gameObject);
			assembled.RB.mass = assembled.Mass;
			
			assembleds[i] = assembled;
		}
	}

	float SubassemblyTotalMass(Creation.SubAssemblyParts asm, List<Construct.Part> parts) {
		float total = 0;
		foreach (var pi in asm.Parts) {
			var part = parts[pi];

			total += CalculatePartMass(part);
		}
		return total;
	}

	float CalculatePartMass(Construct.Part part) {
		float total = 0;
		// iterate through tris
		var triposes = part.GetBasePart().AllTriPositions;
		part.TransformPoints(triposes);

		for (int i = 0; i < triposes.Length; i += 3) {
			Vector3 p1 = triposes[i + 0];
			Vector3 p2 = triposes[i + 1];
			Vector3 p3 = triposes[i + 2];

			total += Vector3.Dot(p1, Vector3.Cross(p2, p3)) / 6f;
		}

		return total;
	}

	List<AxleConnection> CalculateAxleJoints(List<Creation.SubAssembled> assembleds, List<Construct.Part> parts) {
		List<int> axleParts = new();
		foreach (var assembly in assembleds)
			axleParts.AddRange(
				assembly.Source.Parts.Where(pi => PartIsAxle(parts[pi])));

		List<AxleConnection> connections = new();

		foreach (int api in axleParts) {
			int assemblyofpart = assembleds
				.First(a => a.Source.Parts.Contains(api)).Source.ID;

			var axle = parts[api] as Part_Axle.CPart;

			for (int connectionI = 0; connectionI < assembleds.Count; connectionI++) {
				Creation.SubAssembled assembled = assembleds[connectionI];
				var subAssembly = assembled.Source;

				if (subAssembly.ID == assemblyofpart) continue; // dont check itself

				if (AxleIntersectionTest(
					subAssembly,
					axle.endAPos,
					axle.endBPos,
					out Vector3 jointPos
					)) {
					// add joint on axle connecting it to the sub's parent

					connections.Add(new() {
						AxleAssembly = assemblyofpart,
						ConnectedAssemblyIndex = connectionI,
						JointPos = jointPos,
						axis = ((Vector3)axle.endBPos - axle.endAPos).normalized
					});
				}
			}
		}

		return connections;
	}

	 static bool AxleIntersectionTest(
		Creation.SubAssemblyParts subassembly,
		Vector3 axleEndA,
		Vector3 axleEndB,
		out Vector3 jointPos) {

		var parts = BuildingManager.Instance.Assembly.Parts;

		// get all intersections between both ends
		Vector3 direction = (axleEndB - axleEndA).normalized;
		List<float> points = new();

		foreach (int pi in subassembly.Parts) {
			var part = parts[pi];

			points.AddRange(PartIntersectionsWithRay(part, axleEndA, direction));
		}

		jointPos = Vector3.zero;

		// dont include intersections that extend outside the range
		float maxDistSquared = (axleEndA - axleEndB).sqrMagnitude;

		var intersectionPoints =
			points
			.Where(t => t * t < maxDistSquared)
			.Select(t => axleEndA + direction * t).ToArray();

		if (intersectionPoints.Length == 0) return false;

		// lots of debugging potential and probably need here :}
		var average = Vector3.zero;
		int count = 0;
		foreach (var point in intersectionPoints) {
			average += point;
			count++;
		}
		average /= count;
		jointPos = average;

		return true;
	}

	static List<float> PartIntersectionsWithRay(
		Part part,
		Vector3 origin,
		Vector3 direction) {

		Triangle[] wsTris = PartUtil.PartToWSTriList(part);

		List<float> dists = new();
		foreach (var tri in wsTris) {
			float intersect = Intersections.RayTriIntersectDist(
				origin,
				direction,
				tri.p1,
				tri.p2,
				tri.p3);

			if (intersect != -1)
				dists.Add(intersect);
		}

		return dists;
	}

	void ApplyAxleConnections(List<AxleConnection> axleConnections, List<Creation.SubAssembled> assembleds) {
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