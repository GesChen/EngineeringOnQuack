using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Geometry;
using UnityEditor.Hardware;
using UnityEngine;
using UnityEngine.UIElements;

public class Assembler : Singleton<Assembler> {
	// rewritten assembler with basically the same methods as before but
	// just better because the old code sucks so much wtf

	public struct AxleConnection {
		public int AxleAssembly;
		public int ConnectedAssembly;
		public Vector3 JointPos;
		public Vector3 axis;
	}

	/// <summary>
	/// the main method
	/// </summary>
	public void Assemble(Construct construct, out Creation creation) {
		var parts = construct.Parts;

		creation = CreateCreation(construct.Name);

		var createdParts = CopyToSimulation(parts);
		
		var groups = FindPartGroups(createdParts);

		var subassemblies = MakeSubAssemblies(groups, creation, createdParts);

		SetupPhysics(subassemblies, parts);

		SetupCreation(creation, construct, subassemblies);

		FinalizeAllParts(creation, createdParts, parts);

		FinalizeNSPSetup(subassemblies, creation);

		RandomizeSAIDS(subassemblies);
	}

	public void ReconstructCreation(Creation.Serializable creation, out Creation created) {
		var allCreationParts = creation.SubAssemblies.SelectMany(sa => sa.Parts).Select(p => p.source).ToList();

		var newCreation = CreateCreation(creation.Construct.Name);

		var createdParts = CopyToSimulation(allCreationParts);

		var groups = creation.SubAssemblies.Select(s => s.Parts.Select(p => p.source.id).ToList()).ToList();

		var subassemblies = MakeSubAssemblies(groups, newCreation, createdParts);

		// re set the ids
		for (int i = 0; i < subassemblies.Count; i++) {
			subassemblies[i].ID = creation.SubAssemblies[i].ID;
		}

		SetupPhysics(subassemblies, allCreationParts);

		SetupCreation(newCreation, creation.Construct, subassemblies);

		FinalizeAllParts(newCreation, createdParts, allCreationParts);

		FinalizeNSPSetup(subassemblies, newCreation);

		// restore creation daata
		newCreation.ID = creation.ID;
		creation.Transform.ApplyToTransform(newCreation.transform);

		// restore sa transforms
		foreach (var sa in creation.SubAssemblies) {
			var newSA = newCreation.SubAssemblies.Find(ncsa => ncsa.ID == sa.ID);

			sa.Transform.ApplyToTransform(newSA.Transform);

			// restore part transforms
			foreach (var part in sa.Parts)
				part.Transform.ApplyToTransform(
					newSA.Parts.Find(np => np.ID == part.source.id).transform);
		}

		created = newCreation;
	}

	Creation CreateCreation(string name) {
		GameObject newObj = new(name);
		newObj.transform.SetParent(GameManager.Instance.CreationsContainer);
		return newObj.AddComponent<Creation>();
	}

	List<Part> CopyToSimulation(List<Construct.Part> Parts) {
		List<Part> created = new();

		foreach (var part in Parts) {
			Transform newObject = Instantiate(part.GetBasePart().Prefab).transform;
			newObject.SetLocalPositionAndRotation(part.position, part.rotation);
			newObject.localScale = part.scale;
			newObject.gameObject.SetActive(true);

			var partComp = newObject.GetComponent<Part>();
			partComp.basePart = part.GetBasePart();
			partComp.ID = part.id;
			partComp.color = part.color;
			partComp.composition = Compositions.Get(part.compositionID);

			partComp.enabled = false;

			created.Add(partComp);
		}

		return created;
	}
	
	// find groups by choosing a part and finding anything that touches it
	List<List<int>> FindPartGroups(List<Part> parts) {

		List<List<int>> groups = new();

		HashSet<int> checkedParts = new();

		foreach (var part in parts) {
			if (checkedParts.Contains(part.ID)) continue;

			var group = GroupCheck(part, parts, checkedParts);

			groups.Add(group);
		}

		return groups;
	}
	List<int> GroupCheck(Part part, List<Part> parts, HashSet<int> checkedParts) {
		checkedParts.Add(part.ID); // add self

		List<int> group = new() {part.ID};

		// test on all nonfound and nonself parts
		foreach (var check in parts) {
			if (check.ID == part.ID
				|| checkedParts.Contains(check.ID)) continue;

			if (TestTwoPartConnection(part, check)) {
				// check further
				group.AddRange(GroupCheck(check, parts, checkedParts));
			}
		}

		return group;
	}

	List<Creation.SubAssembly> MakeSubAssemblies(List<List<int>> groups, Creation creation, List<Part> parts) {

		List<Creation.SubAssembly> subs = new();

		foreach (var group in groups) {
			var sub = GenerateSA(group, creation, parts);
			subs.Add(sub);
		}

		return subs;
	}

	Creation.SubAssembly GenerateSA(List<int> group, Creation creation, List<Part> parts) {
		Transform subParent = new GameObject($"SubAssembly ({group.Count})").transform;
		subParent.SetParent(creation.transform);

		List<Part> subParts = new();
		Vector3 accumPos = Vector3.zero;

		foreach (var partid in group) {
			var gpart = parts.Find(p => p.ID == partid);

			subParts.Add(gpart);

			accumPos += gpart.transform.position;
		}

		// doesnt matter where it is but might as well
		subParent.position = accumPos / group.Count;
		foreach (var gpart in subParts)
			gpart.transform.parent = subParent;

		return new() {
			Transform = subParent,
			Parts = subParts
		};
	}

	void SetupPhysics(List<Creation.SubAssembly> assembleds, List<Construct.Part> parts) {
		AddRBs(assembleds);

		CalculateAssemblyMasses(assembleds, parts);

		var joints = CalculateAxleJoints(assembleds);

		ApplyAxleConnections(joints, assembleds);
	}

	void SetupCreation(Creation creation, Construct construct, List<Creation.SubAssembly> assembleds) {
		creation.SubAssemblies = assembleds;
		creation.Construct = construct;
		creation.Outputs = construct.Outputs.Select(o =>
			new Output() {
				Name = o
			}).ToList();
		creation.ID = HF.GenerateUID();
	}

	void FinalizeAllParts(Creation creation, List<Part> parts, List<Construct.Part> source) {
		for (int i = 0; i < parts.Count; i++) {
			Part newPart = parts[i];
			Construct.Part origPart = source[i];

			origPart.FinalizeInstantiation(newPart.gameObject, creation.gameObject);
		}
	}

	void FinalizeNSPSetup(List<Creation.SubAssembly> subs, Creation creation) {
		// set all nsp creationids
		foreach (var sub in subs)
			foreach (var part in sub.Parts)
				if (part.IsNonStaticPart(out var nsp)) {
					nsp.CreationID = creation.ID;
					nsp.BecomeAssembled();
				}
	}

	void RandomizeSAIDS(List<Creation.SubAssembly> subs) {
		foreach (var sub in subs)
			sub.ID = HF.GenerateUID();
	}

	// --------- helper functs-----------------
	
	// method does extra check for axles in the following manner: ---------
	// if both parts are normal, just check for intersect
	// if one is axle, perform normal axle check
	// if both are axles, do intersection check i guess? like normal both parts
	bool TestTwoPartConnection(Part A, Part B) {
		bool aIsAxle = A.IsNonStaticPart(out var ansp) && ansp is Part_Axle;
		bool bIsAxle = B.IsNonStaticPart(out var bnsp) && bnsp is Part_Axle;

		static Vector3[] WSVerts(Part part) {
			Vector3[] verts = part.basePart.AllVerts;
			part.transform.TransformPoints(verts);
			return verts;
		}

		if (aIsAxle == bIsAxle) { // both are either part or axle
								  // so do normal meshes
			Vector3[] AWSVerts = WSVerts(A);
			Vector3[] BWSVerts = WSVerts(B);
			int[] Atris = A.basePart.AllTris;
			int[] Btris = B.basePart.AllTris;

			return Intersections.MeshesIntersectRawMesh(AWSVerts, BWSVerts, Atris, Btris);

		} else { // one is axle since they are different
			Part axlePart = aIsAxle ? A : B;
			Part nonPart = aIsAxle ? B : A;

			// only connect if either end of axle is inside the normal
			axlePart.IsNonStaticPart(out var nsp);
			Part_Axle axle = nsp as Part_Axle;

			Vector3[] nonVerts = WSVerts(nonPart);
			Triangle[] partTris = Triangle.FromVertexArray(
				nonVerts,
				nonPart.basePart.AllTris);

			Vector3 pointA = axle.endA.position;
			Vector3 pointB = axle.endB.position;

			if (Intersections.PointInMesh(pointA, partTris)) return true;
			if (Intersections.PointInMesh(pointB, partTris)) return true;

			return false;
		}
	}

	void CalculateAssemblyMasses(List<Creation.SubAssembly> assembleds, List<Construct.Part> parts) {
		for (int i = 0; i < assembleds.Count; i++) {
			Creation.SubAssembly assembled = assembleds[i];

			assembled.Mass = SubassemblyTotalMass(assembleds[i], parts);
			assembled.RB.mass = assembled.Mass;

			assembleds[i] = assembled;
		}
	}

	void AddRBs(List<Creation.SubAssembly> assembleds) {
		for (int i = 0; i < assembleds.Count; i++) {
			Creation.SubAssembly assembled = assembleds[i];

			assembled.RB = HF.GetOrMakeRigidBody(assembled.Transform.gameObject);
			assembled.RB.mass = assembled.Mass;
			
			assembleds[i] = assembled;
		}
	}

	float SubassemblyTotalMass(Creation.SubAssembly asm, List<Construct.Part> parts) {
		float total = 0;
		foreach (var part in asm.Parts) {
			total += CalculatePartMass(part);
		}
		return total;
	}

	float CalculatePartMass(Part part) {
		float total = 0;
		// iterate through tris
		var triposes = part.basePart.AllTriPositions;
		part.transform.TransformPoints(triposes);

		for (int i = 0; i < triposes.Length; i += 3) {
			Vector3 p1 = triposes[i + 0];
			Vector3 p2 = triposes[i + 1];
			Vector3 p3 = triposes[i + 2];

			total += Vector3.Dot(p1, Vector3.Cross(p2, p3)) / 6f;
		}

		return total;
	}

	List<AxleConnection> CalculateAxleJoints(List<Creation.SubAssembly> assembleds) {
		var axles = 
			assembleds.SelectMany((a, i) => 
				a.Parts.Select(p => p.GetComponent<Part_Axle>())
				.Where(ax => ax != null)
				.Select(ax => (ax, i))
			).ToList();

		List<AxleConnection> connections = new();

		foreach (var (axle, assembly) in axles) {
			for (int i = 0; i < assembleds.Count; i++) {
				Creation.SubAssembly subAssembly = assembleds[i];
				if (subAssembly.ID == assembleds[assembly].ID) continue; // dont check itself

				if (AxleIntersectionTest(
					subAssembly,
					axle.endA.position,
					axle.endB.position,
					out Vector3 jointPos
					)) {
					// add joint on axle connecting it to the sub's parent

					connections.Add(new() {
						AxleAssembly = assembly,
						ConnectedAssembly = i,
						JointPos = jointPos,
						axis = (axle.endB.position - axle.endA.position).normalized
					});
				}
			}
		}

		return connections;
	}

	static bool AxleIntersectionTest(
		Creation.SubAssembly subassembly,
		Vector3 axleEndA,
		Vector3 axleEndB,
		out Vector3 jointPos) {

		var parts = BuildingManager.Instance.Assembly.Parts;

		// get all intersections between both ends
		Vector3 direction = (axleEndB - axleEndA).normalized;
		List<float> points = new();

		foreach (var part in subassembly.Parts) {
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

	void ApplyAxleConnections(List<AxleConnection> axleConnections, List<Creation.SubAssembly> assembleds) {
		foreach (var ac in axleConnections) {
			int assembly = ac.AxleAssembly;
			var parentsub = assembleds[assembly].Transform;
			var joint = parentsub.gameObject.AddComponent<HingeJoint>();

			int connectedIndex = ac.ConnectedAssembly;
			joint.connectedBody = assembleds[connectedIndex].RB;

			joint.anchor = parentsub.InverseTransformPoint(ac.JointPos);

			joint.axis = ac.axis;
		}
	}
}