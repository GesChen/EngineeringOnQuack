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
	}

	public void Assemble(BuildingManager bm)
	{
		Debug.Log("assembling"); 
		if (bm == null) return;

		List<Connection> connections = new();

		foreach (Part partA in bm.Parts)
		{
			foreach (Part partB in bm.Parts)
			{
				
				if (partA == partB) continue;
				
				bool anyConnection = connections.Any(c =>
					(c.objA == partA.transform && c.objB == partB.transform) ||
					(c.objA == partB.transform && c.objB == partA.transform));
				if (anyConnection) continue;

				if (Intersections.MeshesIntersect(partA.transform, partB.transform))
					connections.Add(new() { objA = partA.transform, objB = partB.transform });
			}
		}

		foreach (Connection connection in connections)
		{
			Debug.Log($"connection between {connection.objA.name} and {connection.objB.name} ");
		}
	}
}
