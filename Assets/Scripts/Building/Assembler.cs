//#define DEBUGMODE

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Assembler : Singleton<Assembler> {
	public BuildingManager bm;

	struct Connection {
		public Transform objA;
		public Transform objB;
		public Part partA;
		public Part partB;
	}
	struct PrecomputeMeshData {
		public Vector3[] verts;
		public int[] tris;
		public Vector3 min;
		public Vector3 max;
	}

	struct Pair {
		public PrecomputeMeshData DataA;
		public PrecomputeMeshData DataB;
		public Part A;
		public Part B;
		public int index;
	}

	public struct Subassembly {
		public List<Part> parts;
	}
	[System.Serializable]
	public struct AssembledSubassembly {
		public Transform parentContainer;
		public List<Transform> parts;
		public Rigidbody rb;
	}

	public void Assemble(out List<Subassembly> computedSubassemblies) {
		bm.ReturnAllPartsToMain();
		List<Subassembly> subassemblies = ComputeAssemblies(bm);
		List<AssembledSubassembly> assembledSubs = CopyToSimulation(subassemblies);
		ReleaseRigidbodies(assembledSubs);

		//SimulationManager.Instance.assembledSubassemblies = assembledSubs;

		computedSubassemblies = subassemblies;
	}

	public List<Subassembly> ComputeAssemblies(BuildingManager bm) {
#if DEBUGMODE
		Debug.Log("assembling");
#endif
		List<Connection> allConnections = FindConnections(bm);
		List<Subassembly> assemblies = ConnectionsToAssemblies(allConnections, bm.Parts);

#if DEBUGMODE
		foreach (Subassembly a in assemblies)
		{
			Debug.Log("assembly: ");
			foreach (Part p in a.parts)
			{
				Debug.Log(p);
			}
		}
#endif

		return assemblies;
	}

	List<Connection> FindConnections(BuildingManager bm) {
		if (bm == null) return new();

		int parts = bm.Parts.Count;
		int numToTest = parts * (parts - 1) / 2;
		List<Connection> connections = new();
		Pair[] pairsTotest = new Pair[numToTest];

		// precompute tri and vert lists
		// while skipping axles
		PrecomputeMeshData[] precomputed = new PrecomputeMeshData[bm.Parts.Count];
		for (int i = 0; i < bm.Parts.Count; i++) {
			Part part = bm.Parts[i];

			Vector3[] verts = PartUtil.WorldSpaceVertsOfPart(part);
			int[] tris = part.basePart.AllTris;

			Vector3 min = Vector3.positiveInfinity;
			Vector3 max = Vector3.negativeInfinity;
			foreach (Vector3 v in verts) {
				min = Vector3.Min(min, v);
				max = Vector3.Max(max, v);
			}

			precomputed[i] = new() {
				tris = tris,
				verts = verts,
				min = min,
				max = max
			};
		}

		// create tests
		int total = 0;
		for (int i = 0; i < parts; i++) {
			for (int j = i + 1; j < parts; j++) {
				pairsTotest[total] = new() {
					DataA = precomputed[i],
					DataB = precomputed[j],
					A = bm.Parts[i],
					B = bm.Parts[j],
					index = total
				};
				total++;
			}
		}

		//Parallel.ForEach(pairsTotest, pair =>
		foreach (Pair pair in pairsTotest) {
			if (Intersections.MeshesIntersectRawMesh(
				pair.DataA.verts, pair.DataB.verts, pair.DataA.tris, pair.DataB.tris)) {
				connections.Add(new() {
					objA = pair.A.transform,
					objB = pair.B.transform,
					partA = pair.A,
					partB = pair.B,
				});
			}
		}
		return connections;
	}

	List<Subassembly> ConnectionsToAssemblies(List<Connection> connections, List<Part> allParts) {
		Dictionary<Part, bool> partsInAssemblies = allParts.ToDictionary(part => part, value => false);
		List<Subassembly> assemblies = new();

		foreach (Connection connection in connections) {
			Part A = connection.partA;
			Part B = connection.partB;

			partsInAssemblies[A] = true;
			partsInAssemblies[B] = true;

			// if no assembly contains part a or b
			bool containsA = assemblies.Any(a => a.parts.Contains(A));
			bool containsB = assemblies.Any(a => a.parts.Contains(B));
			if (!(containsA || containsB)) {
				Subassembly newAssembly = new() { parts = new() { A, B } };
				assemblies.Add(newAssembly);
			} else {
				int assemblyIndex = -1;
				if (containsA) assemblyIndex = assemblies.FindIndex(a => a.parts.Contains(A));
				if (containsB) assemblyIndex = assemblies.FindIndex(a => a.parts.Contains(B)); // could be optimized but im lazy + it looks better

				if (!containsA) assemblies[assemblyIndex].parts.Add(A);
				if (!containsB) assemblies[assemblyIndex].parts.Add(B);
			}
		}

		List<Part> partsLeft = allParts.Where(part => partsInAssemblies[part] == false).ToList();
		foreach (Part part in partsLeft) {
			Subassembly sub = new() { parts = new() { part } };

			assemblies.Add(sub); // solo parts become own assembly
		}

		return assemblies;
	}

	public List<AssembledSubassembly> CopyToSimulation(List<Subassembly> subs) {
		List<AssembledSubassembly> assembleds = new();
		foreach (Subassembly sub in subs) {

			Transform subParent = new GameObject("subassembly").transform;
			subParent.parent = bm.SimulationContainer;
			
			List<Transform> parts = new();
			Vector3 accumPos = Vector3.zero;
			
			foreach (Part part in sub.parts) {
				Transform newObject = Instantiate(part.gameObject).transform;

				newObject.gameObject.SetActive(true);
				var partComp = newObject.GetComponent<Part>();
				partComp.enabled = false;

				bm.Parts.Remove(partComp);

				parts.Add(newObject);

				accumPos += newObject.transform.position;
			}
			subParent.position = accumPos / sub.parts.Count;
			foreach (Transform part in parts)
				part.parent = subParent;

			assembleds.Add(new() {
				parentContainer = subParent,
				parts = parts
			});
		}

		return assembleds;
	}

	public void ReleaseRigidbodies(List<AssembledSubassembly> assembledSubs) {
		for (int i = 0; i < assembledSubs.Count; i++) {
			AssembledSubassembly sub = assembledSubs[i];

			Rigidbody rb = sub.parentContainer.gameObject.AddComponent<Rigidbody>();
			rb.velocity = Vector3.zero;
			sub.rb = rb;
		}
	}

	public void ComputeAxles(List<AssembledSubassembly> assembledSubs) {
		
	}
}
/* old inefficient attempts, might delete
IEnumerator AssembleCoroutine(BuildingManager bm)
{
	UnityEngine.Debug.Log("coroutine assembling");
	if (bm == null) yield break;

	List<Connection> connections = new();

	int parts = bm.Parts.Count;
	int toTest = bm.Parts.Count * (bm.Parts.Count - 1) / 2;
	int tested = 0;

	float start = Time.time;
	for (int i = 0; i < parts; i++)
	{
		for (int j = i + 1; j < parts; j++)
		{
			Part partA = bm.Parts[i];
			Part partB = bm.Parts[j];

			if (partA == partB) continue;

			bool anyConnection = connections.Any(c =>
				(c.objA == partA.transform && c.objB == partB.transform) ||
				(c.objA == partB.transform && c.objB == partA.transform));
			if (anyConnection) continue;

			if (partA.transform.position == partB.transform.position || // extremely strange edge case that causes 700 ms spikes
				Intersections.MeshesIntersect(partA.transform, partB.transform))
				connections.Add(new() { objA = partA.transform, objB = partB.transform });

			tested++;
			//UnityEngine.Debug.Log($"Progress: {(float)tested / toTest * 100}% dt {Time.deltaTime} fps {1/Time.deltaTime}");

			if (1 / Time.deltaTime < Config.FpsLimit - 10) // arbitrary slowdown limit
			{
				//UnityEngine.Debug.Log("too slow, waiting a frame");
				yield return null;
			}
		}
		yield return null;
	}

	UnityEngine.Debug.Log($"coroutine took {Time.time - start} seconds");

	foreach (Connection connection in connections)
	{
		UnityEngine.Debug.Log($"connection between {connection.objA.name} and {connection.objB.name} ");


	}
}
IEnumerator AssembleCoroutineParallel(BuildingManager bm)
{
	UnityEngine.Debug.Log("coroutine parallel assembling");
	if (bm == null) yield break;
	Stopwatch sw = Stopwatch.StartNew();

	int parts = bm.Parts.Count;
	int numToTest = parts * (parts - 1) / 2;
	ConcurrentBag<Connection> connections = new();
	Pair[] pairsTotest = new Pair[numToTest];

	// precompute tri and vert lists
	PrecomputeMeshData[] precomputed = new PrecomputeMeshData[bm.Parts.Count];
	for (int i = 0; i < bm.Parts.Count; i++)
	{
		Transform obj = bm.Parts[i].transform;
		Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
		Vector3[] verts = mesh.vertices;
		for (int v = 0; v < verts.Length; v++)
			verts[v] = obj.TransformPoint(verts[v]);
		int[] tris = mesh.triangles;

		precomputed[i] = new() { tris = tris, verts = verts };
	}

	// create tests
	int total = 0;
	for (int i = 0; i < parts; i++)
	{
		Pair[] localLoopPairsToTest = new Pair[parts - i - 1];
		int localLoopIndex = 0;
		for (int j = i + 1; j < parts; j++)
		{
			localLoopPairsToTest[localLoopIndex] = new()
			{
				Averts = precomputed[i].verts,
				Atris = precomputed[i].tris,
				Bverts = precomputed[j].verts,
				Btris = precomputed[j].tris,
				A = bm.Parts[i].transform,
				B = bm.Parts[j].transform,
				index = total
			};
			total++;
			localLoopIndex++;
		}

		Parallel.ForEach(localLoopPairsToTest, pair =>
		{
			if (Intersections.MeshesIntersect(pair.Averts, pair.Bverts, pair.Atris, pair.Btris))
			{
				connections.Add(new() { objA = pair.A, objB = pair.B });
			}
		});
		yield return null;
	}

	sw.Stop();
	UnityEngine.Debug.Log($"coroutine parallel took {sw.ElapsedMilliseconds} ms");

	foreach (Connection connection in connections)
	{
		UnityEngine.Debug.Log($"connection between {connection.objA.name} and {connection.objB.name} ");


	}
}
}
*/