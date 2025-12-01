#define DEBUGMODE

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snapping {
	// should be 20 but lower for performance
	public static int DefaultPrecision = 20; // for the 24 bits in a float, lower if it is impacting performance
	public static int MaxSteps = 20;

	/// <summary>
	/// Hack that works very fast except with potential intersections
	/// </summary>
	public static bool FastSnap(Part part, bool alt = false) {
		var cam = Camera.main;

		return FastSnap(part, cam.transform.position, cam.transform.forward, alt);
	}
	public static bool FastSnap(Part part, Vector3 rayOrig, Vector3 rayDir, bool alt = false) {
		// find the hit
		if (!Physics.Raycast(
			new Ray(rayOrig, rayDir),
			out var hit,
			Mathf.Infinity,
			1 << LayerMask.NameToLayer("Part"))) {
			return false;
		}

		// find farthest point according to ray 

		if (alt) {
			rayOrig = part.transform.position;
			rayDir = -hit.normal;
		}

		var verts = part.basePart.AllVerts;

		part.transform.TransformPoints(verts);

		var tris = part.basePart.AllTris;

		float dist = -1;
		for (int i = 0; i < tris.Length; i += 3) {
			dist = Mathf.Max(dist,
				Intersections.RayTriIntersectDist(rayOrig, rayDir,
				verts[tris[i + 0]],
				verts[tris[i + 1]],
				verts[tris[i + 2]]));
		}

		Vector3 partPoint = rayOrig + rayDir * dist;

#if DEBUGMODE
		DebugExtra.DrawPoint(partPoint, color: Color.red);
		DebugExtra.DrawPoint(hit.point, color: Color.green);
#endif

		part.transform.position += hit.point - partPoint;
		return true;
	}

	public static bool Snap(Part part) {
		return Snap(part, Camera.main, DefaultPrecision);
	}

	// returns if it actually snapped
	public static bool Snap(Part part, Camera cam, int precision) {
		Transform obj = part.transform;
		Vector3[] verts = part.basePart.AllVerts;
		int[] tris = part.basePart.AllTris;

		Vector3 origPos = obj.position;
		// make sure camera can see object, otherwise it wont work anyway
		if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(cam), obj.GetComponent<Renderer>().bounds)) {
			Debug.LogWarning("not in bounds");
			return false;
		}

		Vector3 direction = (obj.position - cam.transform.position).normalized;

		// transforms the object could possibly hit while snapping
		List<Transform> possibleCollisions = ObjectsBehindSSBounds(verts, obj, cam, DefaultPrecision, out float closest);
#if DEBUGMODE
		Debug.Log(possibleCollisions.Count);
#endif
		if (possibleCollisions.Count == 0) {
#if DEBUGMODE
			Debug.LogWarning("no possible collisions");
#endif
			return false; // nothing possible to hit, don't do anything
		}

		// all possible collisions should be parts
		Part[] possibleColParts = possibleCollisions.Select(t => t.GetComponent<Part>()).ToArray();

		// inital step definition, how much the obj will move each step
		float step = FarthestVertDistFromCamera(verts, obj, cam) - ClosestVertDistFromCamera(verts, obj, cam);
		//step *= 5f / 6f; // make step smaller to avoid skipping over small objects

		//obj.position += direction * closest;

		// begin stepping from the camera?
		obj.position = cam.transform.position;

		// part 1: find closest collision
		int stepsTaken = 0;
		bool didCollide = false;
		while (stepsTaken < MaxSteps && !didCollide) {
			obj.position += direction * step; // take a step
			foreach (var p in possibleColParts) {
				var pVerts = p.basePart.AllVerts;
				var pTris = p.basePart.AllTris;

				if (Intersections.MeshesIntersectRawMesh(verts, pVerts, tris, pTris)) {
					didCollide = true;
					break;
				}
			}
			stepsTaken++;
		}

		if (!didCollide) {
			obj.position = origPos;
			return false; // didn't collide, don't continue
		}

		// part 2: refine 
		bool isColliding;
		bool everCollided = false;
		for (int i = 0; i < precision; i++) {
			step /= 2;
			isColliding = false;
			foreach (var p in possibleColParts) {
				var pVerts = p.basePart.AllVerts;
				var pTris = p.basePart.AllTris;

				if (Intersections.MeshesIntersectRawMesh(verts, pVerts, tris, pTris)) {
					isColliding = true;
					everCollided = true;
					break;
				}
			}
			if (isColliding) { // step back
				obj.position -= direction * step;
			} else { // step forward
				obj.position += direction * step;
			}
		}

		if (!everCollided) { // ?????
			obj.position = origPos;
			return false;
		}

		return true;
	}
	/*
	public static IEnumerator SnapCo(Transform obj, Camera cam, int precision) {
		// make sure camera can see object, otherwise it wont work anyway
		if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(cam), obj.GetComponent<Renderer>().bounds)) {
			Debug.LogWarning("not in bounds");
			yield break;
		}

		Vector3 direction = (obj.position - cam.transform.position).normalized;

		foreach (Collider collider in obj.GetComponents<Collider>())
			collider.enabled = false;

		// transforms the object could possibly hit while snapping
		List<Transform> possibleCollisions = ObjectsBehindSSBounds(obj, cam, DefaultPrecision, out float closest);
		Debug.Log($"closest {closest}");

		// inital step definition, how much the obj will move each step
		float step = FarthestVertDistFromCamera(mesh, obj, cam) - ClosestVertDistFromCamera(mesh, obj, cam);
		//step *= 5f / 6f; // make step smaller to avoid skipping over small objects

		obj.position += direction * closest;
		*/

	/*
		// part 1: find closest collision
		int stepsTaken = 0;
		bool didCollide = false;
		while (stepsTaken < 20 && !didCollide)
		{
			obj.position += cam.transform.forward * step; // take a step
			foreach (Transform t in possibleCollisions)
			{
				if (Intersections.MeshesIntersect(obj, t))
				{
					didCollide = true;
					break;
				}
			}
			stepsTaken++;
		}

		if (!didCollide)
		{
			yield break; // didn't collide, don't continue
		}*/
	
	/*

		Debug.Break();
		// part 2: refine 
		bool isColliding;
		for (int i = 0; i < precision; i++) {
			yield return null;
			step /= 2;
			isColliding = false;
			foreach (Transform t in possibleCollisions) {
				if (Intersections.MeshesIntersect(obj, t)) {
					isColliding = true;
					break;
				}
			}
			Debug.Log(isColliding);
			if (isColliding) { // step back
				obj.position -= direction * step;
			} else { // step forward
				obj.position += direction * step;
			}
		}

		foreach (Collider collider in obj.GetComponents<Collider>())
			collider.enabled = true;
	}
*/
	// returns all objects that are behind the SS bounds of `obj` from `cam` perspective
	private static List<Transform> ObjectsBehindSSBounds(Vector3[] verts, Transform obj, Camera cam, int gridDensity, out float closestDist) {
		// find screen space bounds of object

		Vector2 min = Vector2.positiveInfinity;
		Vector2 max = Vector2.negativeInfinity;

		foreach (Vector3 pos in verts) {
			Vector3 p = obj.TransformPoint(pos); // world space
			Vector2 SS = cam.WorldToScreenPoint(p); // screen space

			min.x = Mathf.Min(min.x, SS.x);
			min.y = Mathf.Min(min.y, SS.y);

			max.x = Mathf.Max(max.x, SS.x);
			max.y = Mathf.Max(max.y, SS.y);
		}

		// generate grid of screen space points
		List<Vector2> pointGrid = new();
		for (int i = 0; i < gridDensity; i++)
			for (int j = 0; j < gridDensity; j++)
				pointGrid.Add(new(
					Mathf.Lerp(min.x, max.x, i / (gridDensity - 1f)),
					Mathf.Lerp(min.y, max.y, j / (gridDensity - 1f))
					));

		// find farthest distance of mesh from camera in camera's direction
		float farthestDist = FarthestVertDistFromCamera(verts, obj, cam) + .01f;
#if DEBUGMODE
		DebugExtra.DrawPoint(cam.transform.position + cam.transform.forward * farthestDist);
#endif
		// cast a grid of rays from that distance
		List<Transform> uniqueHits = new();
		closestDist = Mathf.Infinity;
		for (int i = 0; i < pointGrid.Count; i++) {
			// center points by subtracting 1/2 of center
			Vector3 withdistance = new(
				pointGrid[i].x,
				pointGrid[i].y,
				farthestDist);

			Vector3 origin = cam.ScreenToWorldPoint(withdistance);

			// use the camera's projection to calculate direction
			withdistance += Vector3.forward;
			Vector3 direction = (cam.ScreenToWorldPoint(withdistance) - origin).normalized;

			Ray ray = new(origin, direction);
#if DEBUGMODE
			DebugExtra.DrawArrow(origin, direction, 1, .1f, Color.red);
#endif
			// handle hits
			// only hit parts
			bool didhit = Physics.Raycast(
				ray,
				out RaycastHit hit,
				Mathf.Infinity,
				1 << LayerMask.NameToLayer("Part"));

			if (didhit) {
				if (hit.distance < closestDist)
					closestDist = hit.distance;

				if (!uniqueHits.Contains(hit.transform) && hit.transform != obj) {
					uniqueHits.Add(hit.transform);
				}
			}
		}

		if (uniqueHits.Count == 0) {
#if DEBUGMODE
			Debug.LogWarning("no hits");
#endif
			closestDist = 0;
		}

		return uniqueHits;
	}

	// gets the distance of the farthest vert from the camera in the camera's forward direction
	private static float FarthestVertDistFromCamera(Vector3[] verts, Transform transform, Camera cam) {
		float farthestDist = Mathf.NegativeInfinity;
		foreach (Vector3 vert in verts) {

			float dist = HF.DistanceInDirection(transform.TransformPoint(vert), cam.transform.position, cam.transform.forward);// (transform.position - cam.transform.position).normalized);
			if (dist > farthestDist) {
				farthestDist = dist;
			}
		}
		return farthestDist;
	}

	// same as farthest, but closest
	private static float ClosestVertDistFromCamera(Vector3[] verts, Transform transform, Camera cam) {
		float closestDist = Mathf.Infinity;
		foreach (Vector3 vert in verts) {
			float dist = HF.DistanceInDirection(transform.TransformPoint(vert), cam.transform.position, cam.transform.forward); // (transform.position - cam.transform.position).normalized);
			if (dist < closestDist) {
				closestDist = dist;
			}
		}
		return closestDist;
	}
}
