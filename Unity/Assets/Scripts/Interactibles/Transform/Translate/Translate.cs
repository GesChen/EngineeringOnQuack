using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class Translate : MonoBehaviour
{
	public Vector3 axes;
	[HideInInspector] public Vector3 localAxes;
	public bool doDynamicBoundsOffset;

	private TransformTools main;
	private Material mat;
	private Renderer objectRenderer;

	bool over;
	bool lastOver;
	bool mouseDown;
	bool lastMouseDown;
	bool lastMainHovering;
	bool hovering;
	bool dragging;

	Color color;

	Vector3 targetIntensity;
	Vector3 smoothedIntensity;
	float targetScale;
	float smoothedScale;
	float targetAlpha;
	float smoothedAlpha;

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
		targetAlpha = main.defaultAlpha;
		targetScale = 1f;
	}

	void Update()
	{
		if (axes != Vector3.one)
			localAxes = main.transform.rotation * axes;

		over = MouseOver();
		mouseDown = main.controls.Transform.Drag.IsPressed();

		bool specialAfterReleaseCase = main.hovering != lastMainHovering;
		if ((over != lastOver || specialAfterReleaseCase) && over)
			StartOver();
		else if (over != lastOver && !over)
			StopOver();

		if (mouseDown != lastMouseDown && mouseDown)
			StartClicking();
		else if (mouseDown != lastMouseDown && !mouseDown)
			StopClicking();

		UpdateVisuals();

		PerformDragging();

		UseAxisIndicator();

		lastOver = over;
		lastMouseDown = mouseDown;
		lastMainHovering = main.hovering;
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
	void StartOver()
	{
		if ((!main.hovering && !dragging) || axes == Vector3.one)
		{
			if (axes == Vector3.one)
				main.specialCenterCase = true;	

			hovering = true;
			main.hovering = true;

			targetIntensity = main.hoverIntensity;
			targetScale = main.hoverScale;
			mat.SetFloat("_TransparentSortPriority", 1);
			HDMaterial.ValidateMaterial(mat);
		}
	}
	void StopOver()
	{
		if (hovering && !dragging)
		{
			hovering = false;
			main.hovering = false;

			targetIntensity = main.defaultIntensity;
			targetScale = 1f;
			mat.SetFloat("_TransparentSortPriority", 0);
			HDMaterial.ValidateMaterial(mat);
		}
		if (axes == Vector3.one)
			main.specialCenterCase = false;
	}
	void StartClicking()
	{
		if (hovering)
		{
			dragging = true;
			main.dragging = true;

			targetIntensity = main.draggingIntensity;
			targetScale = main.draggingScale;

			// axis indicator code here if going to use 

			mouseOffset = main.controls.Transform.MousePos.ReadValue<Vector2>() - (Vector2)Camera.main.WorldToScreenPoint(transform.position);
			distance = Vector3.Distance(transform.position, Camera.main.transform.position);
			dragStartPos = main.transform.position;
		}
	}
	void StopClicking()
	{
		if (!dragging) return;

		main.transform.position = main.target.position;
		transform.localPosition = axes == Vector3.one ? Vector3.zero : axes;

		main.axisIndicator.inUse = false;
		main.axisIndicator.transform.localScale = new(.015f, .015f, 2f);

		dragging = false;
		main.dragging = false;
		if (over)
		{
			hovering = true;
			main.hovering = true;
			targetIntensity = main.hoverIntensity;
			targetScale = main.hoverScale;
		}
		else
		{
			hovering = false;
			main.hovering = false;
			targetIntensity = main.defaultIntensity;
			targetScale = 1f;
			main.specialCenterCase = false;
		}
	}
	void UpdateVisuals()
	{
		if (main.dragging && !dragging) targetAlpha = main.draggingAlpha;
		else if (main.hovering && !hovering) targetAlpha = main.notHoveredAlpha;
		else targetAlpha = main.defaultAlpha;

		// smoothing, can use different fucntions
		smoothedIntensity = Vector3.Lerp(smoothedIntensity, targetIntensity, main.intensitySmoothness);
		smoothedScale = Mathf.Lerp(smoothedScale, targetScale, main.scaleSmoothness);
		smoothedAlpha = Mathf.Lerp(smoothedAlpha, targetAlpha, main.alphaSmoothness);

		mat.SetColor("_EmissiveColor", HelperFunctions.MultiplyColorByVector(smoothedIntensity, color));
		mat.color = new(color.r, color.g, color.b, smoothedAlpha);
		//mat.SetFloat("_Alpha", smoothedAlpha);
		transform.localScale = smoothedScale * Vector3.one;
	}
	void PerformDragging()
	{
		if (!dragging) return;

		Camera mainCamera = Camera.main;

		int numaxes = (int)(axes.x + axes.y + axes.z);
		Vector3 mouseScreenSpace = main.controls.Transform.MousePos.ReadValue<Vector2>() - mouseOffset;
		mouseScreenSpace.z = mainCamera.nearClipPlane;

		Vector3 cameraPos = mainCamera.transform.position;
		Vector3 cameraVec = mainCamera.ScreenToWorldPoint(mouseScreenSpace) - cameraPos;

		switch (numaxes)
		{
			case 1:
				transform.position = HelperFunctions.ClosestPointAOnTwoLines(
					dragStartPos, localAxes.normalized,
					cameraPos, cameraVec.normalized); //alot of hacky stuff going on here that i dont understand
				break;
			case 2:
				transform.position = HelperFunctions.RayPlaneIntersect(
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
	void UseAxisIndicator()
	{
		if (dragging && (axes.x + axes.y + axes.z) == 1)
		{
			main.axisIndicator.inUse = true;
			main.axisIndicator.color = color;
			main.axisIndicator.transform.position = (transform.position + dragStartPos) / 2;
			main.axisIndicator.transform.localScale = new(.015f, .015f, (transform.position - dragStartPos).magnitude * 2f + main.axisIndicatorLengthOffset);
			main.axisIndicator.rotation = Quaternion.LookRotation(localAxes, transform.up);
		}
	}
}