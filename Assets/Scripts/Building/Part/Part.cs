using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part : MonoBehaviour
{
	public bool Selected;
	public Mesh processingMesh;
	[HideInInspector] public Vector3[] allVerts;
	[HideInInspector] public int[] allTris;
	[HideInInspector] public Vector3[] allTriPositions;

	void Awake()
	{
		if (processingMesh == null)
		{
			Debug.LogError($"processingmesh is null, defaulting to meshfilter mesh");
			processingMesh = GetComponent<MeshFilter>().mesh;
		}

		List<Vector3> all = new();
		GetAllTriPositions(transform, ref all);
		allTriPositions = all.ToArray();

		List<int> allIndexes = new();
		GetAllTris(transform, ref allIndexes);
		allTriPositions = all.ToArray();

		all = new();
		GetMeshVertices(transform, ref all);
		allVerts = all.ToArray();
	}

	void GetAllTriPositions(Transform target, ref List<Vector3> allTriPoses)
	{
		if (target.TryGetComponent(out Part part))
		{
			Mesh mesh = part.processingMesh;
			if (mesh != null)
			{
				Vector3[] verts = mesh.vertices;
				foreach (int i in mesh.triangles)
					allTriPoses.Add(verts[i]); // Add vertices to the combined list
			}
		}

		// Recursively iterate through children
		foreach (Transform child in target.transform)
		{
			GetAllTriPositions(child, ref allTriPoses);
		}
	}

	void GetAllTris(Transform target, ref List<int> allTris)
	{
		if (target.TryGetComponent(out Part part))
		{
			Mesh mesh = part.processingMesh;
			if (mesh != null)
			{
				Vector3[] verts = mesh.vertices;
				foreach (int i in mesh.triangles)
					allTris.Add(i); // Add vertices to the combined list
			}
		}

		// Recursively iterate through children
		foreach (Transform child in target.transform)
		{
			GetAllTris(child, ref allTris);
		}
	}


	void GetMeshVertices(Transform target, ref List<Vector3> allVertices)
	{
		if (target.TryGetComponent(out Part part))
		{
			Mesh mesh = part.processingMesh;
			if (mesh != null)
			{
				allVertices.AddRange(mesh.vertices); // Add vertices to the combined list
			}
		}

		// Recursively iterate through children
		foreach (Transform child in target.transform)
		{
			GetMeshVertices(child, ref allVertices);
		}
	}

	// Update is called once per frame
	void Update()
	{
		if (Selected)
		{
			gameObject.layer = LayerMask.NameToLayer("Selected");
		}
		else
		{
			gameObject.layer = LayerMask.NameToLayer("Part");
		}
	}
}
