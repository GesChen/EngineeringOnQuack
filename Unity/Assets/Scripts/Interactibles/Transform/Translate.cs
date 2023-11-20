using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Translate : MonoBehaviour
{
	public Vector3 axes;
	[HideInInspector] public Vector3 localAxes;
	public bool doDynamicBoundsOffset;

	private TransformTools main;
	private Material mat;
	private Renderer objectRenderer;

	Color color;

	float targetIntensity;
	float smoothedIntensity;

	float targetScale;

	bool isBeingHovered;
	bool isBeingDragged;
	bool lastBeingDragged;
	Vector3 dragStartPos;

	float distance;
	Vector2 mouseOffset;

	void Awake()
	{
		main = GetComponentInParent<TransformTools>();
		objectRenderer = GetComponent<Renderer>();
		mat = objectRenderer.material;
		color = mat.color;

		targetIntensity = main.defaultIntensity;
		smoothedIntensity = main.defaultIntensity;

		targetScale = 1f;
	}

	void Update()
	{
		if (axes != Vector3.one)
			localAxes = main.transform.rotation * axes;
		
		if ((main.dragging || main.hovering) && !(isBeingHovered || isBeingDragged)) //skip processing if not needed
		{
			transform.localPosition = axes == Vector3.one ? Vector3.zero : axes;
			// scale out if mouse is moving slow enough 
			if (main.controls.Transform.MouseDelta.ReadValue<Vector2>().sqrMagnitude < Mathf.Pow(main.maxMouseSpeedToScaleOut, 2))
				transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, main.scaleSmoothness);
			return;
		}
		
		isBeingHovered = MouseOver();
		main.hovering = isBeingHovered;

		if (isBeingHovered && main.controls.Transform.Drag.IsPressed())
		{
			isBeingDragged = true;
			main.dragging = true;
		}
		// handle transition if in bounds
		if (isBeingHovered)
		{
			targetIntensity = main.hoverIntensity;
			targetScale = main.hoverScale;
		}
		else
		{
			targetIntensity = main.defaultIntensity;
			targetScale = 1;
		}
			
		if (!main.controls.Transform.Drag.IsPressed())
		{
			isBeingDragged = false;
			main.dragging = false;
		}

		// hand drag state changes
		if (isBeingDragged != lastBeingDragged)
		{
			// start drag
			if (isBeingDragged)
			{
				mouseOffset = main.controls.Transform.MousePos.ReadValue<Vector2>() - (Vector2)Camera.main.WorldToScreenPoint(transform.position);
				distance = Vector3.Distance(transform.position, Camera.main.transform.position);
				dragStartPos = main.transform.position;
			}
			// end drag
			else if (!isBeingDragged)
			{
				main.transform.position = main.target.position;
				transform.localPosition = axes == Vector3.one ? Vector3.zero : axes;
			}
		}

		if (isBeingDragged)
		{
			PerformDragging();
			targetIntensity = main.draggingIntensity;
			targetScale = main.draggingScale;
		}

		// handle actual lerping outside of if statement
		smoothedIntensity = Mathf.Lerp(smoothedIntensity, targetIntensity, main.intensitySmoothness);

		mat.SetColor("_EmissiveColor", color * smoothedIntensity);
		transform.localScale = Vector3.Lerp(transform.localScale, targetScale * Vector3.one, main.scaleSmoothness); ;
		lastBeingDragged = isBeingDragged;
	}

	bool MouseOver()
	{
		// get world bounds and camera
		Bounds bounds = objectRenderer.bounds;
		Camera mainCamera = Camera.main;

		// calculate screen point positions of those bounds
		Vector3[] boundsCorners = new Vector3[8];
		boundsCorners[0] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, 1, 1)));
		boundsCorners[1] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, 1, 1)));
		boundsCorners[2] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, -1, 1)));
		boundsCorners[3] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, 1, -1)));
		boundsCorners[4] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, -1, 1)));
		boundsCorners[5] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, -1, -1)));
		boundsCorners[6] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, 1, -1)));
		boundsCorners[7] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, -1, -1)));

		// calculate the bounds of the screen space bounds
		Vector2 minScreen = Vector2.positiveInfinity;
		Vector2 maxScreen = Vector2.negativeInfinity;

		foreach (Vector2 corner in boundsCorners)
		{
			minScreen = Vector2.Min(minScreen, corner);
			maxScreen = Vector2.Max(maxScreen, corner);
		}

		Vector2 mousePos = main.controls.Transform.MousePos.ReadValue<Vector2>();
		bool inBounds;

		// dynamic bounds offset
		if (doDynamicBoundsOffset)
		{
			float dot = Vector3.Dot(Vector3.one - localAxes, Camera.main.transform.forward);
			float dynamicOffset = -((1 - dot) * 2 - 1) * main.boundsOffset;

			inBounds = mousePos.x >= minScreen.x + dynamicOffset && mousePos.x <= maxScreen.x - dynamicOffset &&
						mousePos.y >= minScreen.y + dynamicOffset && mousePos.y <= maxScreen.y - dynamicOffset;
		}
		else
		{
			// determine if mouse is inside ss bounds
			inBounds = mousePos.x >= minScreen.x + main.boundsOffset && mousePos.x <= maxScreen.x - main.boundsOffset &&
						mousePos.y >= minScreen.y + main.boundsOffset && mousePos.y <= maxScreen.y - main.boundsOffset;
		}
		return inBounds;
	}

	void PerformDragging()
	{
		Camera mainCamera = Camera.main;

		int numaxes = (int)(axes.x + axes.y + axes.z);
		Vector3 mouseScreenSpace = main.controls.Transform.MousePos.ReadValue<Vector2>() - mouseOffset;
		mouseScreenSpace.z = mainCamera.nearClipPlane;

		Vector3 cameraPos = mainCamera.transform.position;
		Vector3 cameraVec = mainCamera.ScreenToWorldPoint(mouseScreenSpace) - cameraPos;

		switch (numaxes)
		{
			case 1:
				transform.position = TransformTools.ClosestPointAOnTwoLines(
					dragStartPos, localAxes.normalized,
					cameraPos, cameraVec.normalized); //alot of hacky stuff going on here that i dont understand
				break;
			case 2:
				transform.position = TransformTools.RayPlaneIntersect(
					dragStartPos, main.transform.rotation * (Vector3.one - axes),
					cameraPos, cameraVec);
				break;
			case 3:
				mouseScreenSpace.z = distance;
				transform.position = mainCamera.ScreenToWorldPoint(mouseScreenSpace);
				break;
		};

		// move target with direction
		main.target.transform.position = transform.position - (axes == Vector3.one ? Vector3.zero : localAxes * main.transform.localScale.x);
	}
}
