using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part : MonoBehaviour
{
    public bool Selected;
    public Vector3[] allTris;
	public Vector3[] allVerts;

    void Awake()
    {
		List<Vector3> all = new();
		GetAllTris(transform, ref all);
		allTris = all.ToArray();

		all = new();
		GetMeshVertices(transform, ref all);
		allVerts = all.ToArray();
    }

	void GetAllTris(Transform target, ref List<Vector3> allTris)
	{
		if (target.TryGetComponent(out MeshFilter meshFilter))
		{
			Mesh mesh = meshFilter.sharedMesh;
			if (mesh != null)
			{
				Vector3[] verts = mesh.vertices;
				foreach (int i in mesh.triangles)
					allTris.Add(verts[i]); // Add vertices to the combined list
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
		if (target.TryGetComponent(out MeshFilter meshFilter))
		{
			Mesh mesh = meshFilter.sharedMesh;
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
