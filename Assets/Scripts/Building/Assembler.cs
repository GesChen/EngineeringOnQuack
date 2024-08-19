using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class Assembler : MonoBehaviour
{
	struct Connection
	{
		public Transform objA;
		public Transform objB;
	}
	struct PrecomputeMeshData
	{
		public Vector3[] verts;
		public int[] tris;
	}

	struct Pair
	{
		public Vector3[] Averts;
		public Vector3[] Bverts;
		public int[] Atris;
		public int[] Btris;
		public Transform A;
		public Transform B;
		public int index;
	}
	

	public void Assemble(BuildingManager bm)
	{
		StartCoroutine(AssembleCoroutine(bm));
		AssembleParallel(bm);
	}

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

	void AssembleParallel(BuildingManager bm)
	{
		UnityEngine.Debug.Log("parallel assembling");
		if (bm == null) return;
		Stopwatch sw = Stopwatch.StartNew();

		int parts = bm.Parts.Count;
		int numToTest = parts * (parts - 1) / 2;
		Connection[] connections = new Connection[numToTest];
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
			for (int j = i + 1; j < parts; j++)
			{
				pairsTotest[total] = new() { 
					Averts = precomputed[i].verts,
					Atris = precomputed[i].tris,
					Bverts = precomputed[j].verts,
					Btris = precomputed[j].tris,
					A = bm.Parts[i].transform, 
					B = bm.Parts[j].transform, 
					index = total };
				total++;
			} 
		}

		
		Parallel.ForEach(pairsTotest, pair =>
		{
			if (Intersections.MeshesIntersect(pair.Averts, pair.Bverts, pair.Atris, pair.Btris))
			{
				connections[pair.index] = new() { objA = pair.A, objB = pair.B };
			}
		}); 

		sw.Stop();
		UnityEngine.Debug.Log($"parallel took {sw.ElapsedMilliseconds} ms");

		/*foreach (Connection connection in connections)
		{
			UnityEngine.Debug.Log($"connection between {connection.objA.name} and {connection.objB.name} ");


		}*/
	}
}
