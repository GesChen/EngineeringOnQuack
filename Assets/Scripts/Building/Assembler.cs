using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Assembler : MonoBehaviour
{
	struct Connection
	{
		public Transform objA;
		public Transform objB;
		public Part partA;
		public Part partB;
	}
	struct PrecomputeMeshData
	{
		public Vector3[] verts;
		public int[] tris;
		public Vector3 min;
		public Vector3 max;
	}

	struct Pair
	{
		public Vector3[] Averts;
		public Vector3[] Bverts;
		public int[] Atris;
		public int[] Btris;
		public Vector3 Amin;
		public Vector3 Bmin;
		public Vector3 Amax;
		public Vector3 Bmax;
		public Part A;
		public Part B;
		public int index;
	}

	public struct Subassembly
	{
		public List<Part> parts;
	}

	public void Assemble(BuildingManager bm)
	{
		List<Subassembly> subassemblies = ComputeAssemblies(bm);
	}

	public List<Subassembly> ComputeAssemblies(BuildingManager bm)
	{
		Debug.Log("assembling");
		List<Connection> allConnections = FindConnections(bm);
		List<Subassembly> assemblies = ConnectionsToAssemblies(allConnections, bm.Parts);

		/*
		foreach (Subassembly a in assemblies)
		{
			Debug.Log("assembly: ");
			foreach (Part p in a.parts)
			{
				Debug.Log(p);
			}
		}
		*/
		return assemblies;
	}

	List<Connection> FindConnections(BuildingManager bm)
	{
		if (bm == null) return new();

		int parts = bm.Parts.Count;
		int numToTest = parts * (parts - 1) / 2;
		List<Connection> connections = new();
		Pair[] pairsTotest = new Pair[numToTest];

		// precompute tri and vert lists
		PrecomputeMeshData[] precomputed = new PrecomputeMeshData[bm.Parts.Count];
		for (int i = 0; i < bm.Parts.Count; i++)
		{
			Part part = bm.Parts[i];
			Transform obj = part.transform;
			Mesh mesh = part.basePart.processingMesh;
			Vector3[] verts = mesh.vertices;
			for (int v = 0; v < verts.Length; v++)
				verts[v] = obj.TransformPoint(verts[v]);
			int[] tris = mesh.triangles;

			Vector3 min = Vector3.positiveInfinity;
			Vector3 max = Vector3.negativeInfinity;
			foreach (Vector3 v in verts)
			{
				min = Vector3.Min(min, v);
				max = Vector3.Max(max, v);
			}

			precomputed[i] = new() { tris = tris, verts = verts, min = min, max = max };
		}

		// create tests
		int total = 0;
		for (int i = 0; i < parts; i++)
		{
			for (int j = i + 1; j < parts; j++)
			{
				pairsTotest[total] = new() {
					Averts = precomputed[i].verts,
					Atris = precomputed[i].tris,
					Bverts = precomputed[j].verts,
					Btris = precomputed[j].tris,
					A = bm.Parts[i],
					B = bm.Parts[j],
					Amax = precomputed[i].max,
					Bmax = precomputed[j].max,
					Amin = precomputed[i].min,
					Bmin = precomputed[j].min,
					index = total };
				total++;
			}
		}

		//Parallel.ForEach(pairsTotest, pair =>
		foreach (Pair pair in pairsTotest)
		{
			if (Intersections.OptimizedMeshesIntersect(
				pair.Averts, pair.Bverts, pair.Atris, pair.Btris,
				pair.Amin, pair.Amax, pair.Bmin, pair.Bmax))
			{
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

	List<Subassembly> ConnectionsToAssemblies(List<Connection> connections, List<Part> allParts)
	{
		Dictionary<Part, bool> partsInAssemblies = allParts.ToDictionary(part => part, value => false);
		List<Subassembly> assemblies = new();

		foreach (Connection connection in connections)
		{
			Part A = connection.partA;
			Part B = connection.partB;

			partsInAssemblies[A] = true;
			partsInAssemblies[B] = true;

			// if no assembly contains part a or b
			bool containsA = assemblies.Any(a => a.parts.Contains(A));
			bool containsB = assemblies.Any(a => a.parts.Contains(B));
			if (!(containsA || containsB))
			{
				Subassembly newAssembly = new() { parts = new() { A, B } };
				assemblies.Add(newAssembly);
			}
			else
			{
				int assemblyIndex = -1;
				if (containsA) assemblyIndex = assemblies.FindIndex(a => a.parts.Contains(A));
				if (containsB) assemblyIndex = assemblies.FindIndex(a => a.parts.Contains(B)); // could be optimized but im lazy + it looks better

				if (!containsA) assemblies[assemblyIndex].parts.Add(A);
				if (!containsB)	assemblies[assemblyIndex].parts.Add(B);
			}
		}

		List<Part> partsLeft = allParts.Where(part => partsInAssemblies[part] == false).ToList();
		foreach (Part part in partsLeft)
		{
			assemblies.Add(new() { parts = new() { part } }); // solo parts become own assembly
		}

		return assemblies;
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