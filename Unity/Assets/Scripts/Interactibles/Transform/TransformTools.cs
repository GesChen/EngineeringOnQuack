using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformTools : MonoBehaviour
{
	public Transform target;
	public float size;
	public float boundsOffset;
	public float maxMouseSpeedToScaleOut;

	[Header("Settings")]
	public float intensitySmoothness = .3f;
	public float scaleSmoothness = .3f;
	public float alphaSmoothness = .1f;

	[Header("Default")]
	public Vector3 defaultIntensity = Vector3.one;
	public float defaultOutset = .04f;
	public float defaultDistance = 10f;
	public float defaultAlpha = .85f;

	[Header("On Hover")]
	public Vector3 hoverIntensity = new Vector3(2, 3, 5);
	public float hoverScale = 1.3f;
	public float hoverOutset = .08f;
	public float hoverDistance = 15f;
	public float notHoveredAlpha = .5f;

	[Header("On Drag")]
	public Vector3 draggingIntensity = new Vector3(3, 4, 6);
	public float draggingScale = 1.2f;
	public float draggingOutset = .07f;

	public bool hovering;
	public bool dragging;

	[HideInInspector] public InputMaster controls;

	void Awake()
	{
		controls = new InputMaster();
	}
	void OnEnable()
	{
		if (controls == null)
			controls = new InputMaster();
		controls.Enable();
	}
	void OnDisable()
	{
		controls.Disable();
	}

	void Update()
	{
		if (!dragging)
			transform.localScale = Vector3.Distance(Camera.main.transform.position, target.position) * size * Vector3.one;
	}
	
	public static Vector3 ClosestPointAOnTwoLines(Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
	{
		Vector3 deltaP = linePoint2 - linePoint1;
		float a = Vector3.Dot(lineVec1, lineVec1);
		float b = Vector3.Dot(lineVec1, lineVec2);
		float c = Vector3.Dot(lineVec2, lineVec2);
		float d = Vector3.Dot(lineVec1, deltaP);
		float e = Vector3.Dot(lineVec2, deltaP);

		float denom = a * c - b * b;

		if (denom == 0)
		{
			// Lines are parallel
			return Vector3.zero;
		}
		float t1 = (b * e - c * d) / denom;
		float t2 = (a * e - b * d) / denom;

		return linePoint1 + t1 * -lineVec1;
		//closestPoint2 = p2 + t2 * v2;
	}
	public static Vector3 RayPlaneIntersect(Vector3 planePoint, Vector3 planeNormal, Vector3 rayOrigin, Vector3 rayDirection)
	{
		float t = Vector3.Dot(planeNormal, planePoint - rayOrigin) / Vector3.Dot(planeNormal, rayDirection);
		Vector3 intersectionPoint = rayOrigin + t * rayDirection;

		float epsilon = 0.0001f;
		if (t < epsilon)
			return Vector3.zero;

		return intersectionPoint;
	}
	public static Color MultiplyColorByVector(Vector3 vector, Color color)
	{
		return new Color(color.r * vector.x, color.g * vector.y, color.b * vector.z, color.a);
	}
}
