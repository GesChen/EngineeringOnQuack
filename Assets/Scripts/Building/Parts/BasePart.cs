using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasePart {
	public int ID;
	public string Name;

	private readonly string BaseMeshPath;
	private Mesh m_baseMesh;
	public Mesh BaseMesh => HF.LoadResource(ref m_baseMesh, BaseMeshPath);

	private readonly string ProcessingMeshPath;
	private Mesh m_processingMesh;
	public Mesh ProcessingMesh => HF.LoadResource(ref m_processingMesh, ProcessingMeshPath);

	private readonly string PrefabPath;
	private GameObject m_prefab;
	public GameObject Prefab => HF.LoadResource(ref m_prefab, PrefabPath);

	private Vector3[] m_allVerts;
	/// <summary>
	/// Processing verts
	/// </summary>
	public Vector3[] AllVerts => // copy
		HF.LoadCached(ref m_allVerts, () => ProcessingMesh.vertices).ToArray();

	private int[] m_allTris; 
	/// <summary>
	/// Processing triangle indices
	/// </summary>
	public int[] AllTris => // copy
		HF.LoadCached(ref m_allTris, () => ProcessingMesh.triangles).ToArray();

	private Vector3[] m_allTriPositions;
	public Vector3[] AllTriPositions => //copy
		HF.LoadCached(ref m_allTriPositions, () => {
			Vector3[] verts = AllVerts;
			return AllTris.Select(i => verts[i]).ToArray();
		}).ToArray();

	/// <summary>
	/// Only give names, paths are resolved automatically. 
	/// </summary>
	public BasePart(
		int id,
		string name,
		string baseMeshPath,
		string processingMeshPath,
		string prefabPath) {

		ID = id;
		Name = name;
		BaseMeshPath =			Config.Locations.BasePartsFolder		+ baseMeshPath;
		ProcessingMeshPath =	Config.Locations.ProcessingPartsFolder	+ processingMeshPath;
		PrefabPath =			Config.Locations.TemplatePartsFolder	+ prefabPath;
	}
}