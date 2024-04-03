//#define DEBUGMODE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Snapping
{
	public static void Snap(Transform obj, Camera cam, int precision)
	{
		Vector3 origPos = obj.position;
		// make sure camera can see object, otherwise it wont work anyway
		if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(cam), obj.GetComponent<Renderer>().bounds))
		{
			Debug.LogWarning("not in bounds");
			return;
		}

		Vector3 direction = (obj.position - cam.transform.position).normalized;

		foreach (Collider collider in obj.GetComponents<Collider>())
			collider.enabled = false;

		// transforms the object could possibly hit while snapping
		List<Transform> possibleCollisions = ObjectsBehindSSBounds(obj, cam, 15, out float closest);
#if DEBUGMODE
		Debug.Log(possibleCollisions.Count);
#endif
		if (possibleCollisions.Count == 0)
		{
#if DEBUGMODE
			Debug.LogWarning("no possible collisions");
#endif
			return; // nothing possible to hit, don't do anything
		}

		// inital step definition, how much the obj will move each step
		Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
		float step = FarthestVertDistFromCamera(mesh, obj, cam) - ClosestVertDistFromCamera(mesh, obj, cam);
		//step *= 5f / 6f; // make step smaller to avoid skipping over small objects

		obj.position += direction * closest;
		
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
			return; // didn't collide, don't continue
		}*/

		// part 2: refine 
		bool isColliding;
		bool everCollided = false;
		for (int i = 0; i < precision; i++)
		{
			step /= 2;
			isColliding = false;
			foreach (Transform t in possibleCollisions)
			{
				if (Intersections.MeshesIntersect(obj, t))
				{
					isColliding = true;
					everCollided = true;
					break;
				}
			}
			if (isColliding)
			{ // step back
				obj.position -= direction * step;
			}
			else
			{ // step forward
				obj.position += direction * step;
			}
		}

		if (!everCollided)
		{
			obj.position = origPos;
		}

		foreach (Collider collider in obj.GetComponents<Collider>())
			collider.enabled = true;
	}
	public static IEnumerator SnapCo(Transform obj, Camera cam, int precision)
	{
		// make sure camera can see object, otherwise it wont work anyway
		if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(cam), obj.GetComponent<Renderer>().bounds))
		{
			Debug.LogWarning("not in bounds");
			yield break;
		}

		Vector3 direction = (obj.position - cam.transform.position).normalized;

		foreach (Collider collider in obj.GetComponents<Collider>())
			collider.enabled = false;

		// transforms the object could possibly hit while snapping
		List<Transform> possibleCollisions = ObjectsBehindSSBounds(obj, cam, 15, out float closest);
		Debug.Log($"closest {closest}");

		// inital step definition, how much the obj will move each step
		Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
		float step = FarthestVertDistFromCamera(mesh, obj, cam) - ClosestVertDistFromCamera(mesh, obj, cam);
		//step *= 5f / 6f; // make step smaller to avoid skipping over small objects

		obj.position += direction * closest;
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

		Debug.Break();
		// part 2: refine 
		bool isColliding;
		for (int i = 0; i < precision; i++)
		{
			yield return null;
			step /= 2;
			isColliding = false;
			foreach (Transform t in possibleCollisions)
			{
				if (Intersections.MeshesIntersect(obj, t))
				{
					isColliding = true;
					break;
				}
			}
			Debug.Log(isColliding);
			if (isColliding)
			{ // step back
				obj.position -= direction * step;
			}
			else
			{ // step forward
				obj.position += direction * step;
			}
		}

		foreach (Collider collider in obj.GetComponents<Collider>())
			collider.enabled = true;
	}

	public static void Snap(Transform obj)
	{
		Snap(obj, Camera.main, 15);
	}

	// returns all objects that are behind the SS bounds of `obj` from `cam` perspective
	private static List<Transform> ObjectsBehindSSBounds(Transform obj, Camera cam, int gridDensity, out float closestDist)
	{
		// find screen space bounds of object

		Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
		Vector3[] vertPositions = mesh.vertices;

		Vector2 min = Vector2.positiveInfinity;
		Vector2 max = Vector2.negativeInfinity;

		foreach (Vector3 pos in vertPositions)
		{
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
		float farthestDist = FarthestVertDistFromCamera(mesh, obj, cam) + .01f;
#if DEBUGMODE
		DebugExtra.DrawPoint(cam.transform.position + cam.transform.forward * farthestDist);
#endif
		// cast a grid of rays from that distance
		List<Transform> uniqueHits = new();
		closestDist = Mathf.Infinity;
		for (int i = 0; i < pointGrid.Count; i++)
		{
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
			bool didhit = Physics.Raycast(ray, out RaycastHit hit);

			if (didhit)
			{
				if (hit.distance < closestDist)
					closestDist = hit.distance;

				if (!uniqueHits.Contains(hit.transform) && hit.transform != obj)
				{
					uniqueHits.Add(hit.transform);
				}
			}
		}

		if (uniqueHits.Count == 0)
		{
#if DEBUGMODE
			Debug.LogWarning("no hits");
#endif
			closestDist = 0;
		}

		return uniqueHits;
	}

	// gets the distance of the farthest vert from the camera in the camera's forward direction
	private static float FarthestVertDistFromCamera(Mesh mesh, Transform transform, Camera cam)
	{
		float farthestDist = Mathf.NegativeInfinity;
		Vector3[] verts = mesh.vertices;
		foreach (Vector3 vert in verts)
		{

			float dist = HelperFunctions.DistanceInDirection(transform.TransformPoint(vert), cam.transform.position, cam.transform.forward);// (transform.position - cam.transform.position).normalized);
			if (dist > farthestDist)
			{
				farthestDist = dist;
			}
		}
		return farthestDist;
	}

	// same as farthest, but closest
	private static float ClosestVertDistFromCamera(Mesh mesh, Transform transform, Camera cam)
	{
		float closestDist = Mathf.Infinity;
		foreach (Vector3 vert in mesh.vertices)
		{
			float dist = HelperFunctions.DistanceInDirection(transform.TransformPoint(vert), cam.transform.position, cam.transform.forward); // (transform.position - cam.transform.position).normalized);
			if (dist < closestDist)
			{
				closestDist = dist;
			}
		}
		return closestDist;
	}
}
