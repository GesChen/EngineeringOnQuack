using Unity.VisualScripting.YamlDotNet.Serialization;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class Translate : MonoBehaviour
{
	public Vector3Int axes;
	[HideInInspector] public Vector3 localAxes;
	public bool doDynamicBoundsOffset;

	private TransformTools main;
	private TranslateMain parentMain;
	private Material mat;
	private Renderer objectRenderer;

	bool over;
	bool lastOver;
	bool mouseDown;
	bool lastMouseDown;
	bool lastMainHovering;
	bool hovering;
	bool dragging;
	float lastMouseDownTime;
	bool resetting;
	int numaxes;

	Color color;

	Vector3 targetIntensity;
	Vector3 smoothedIntensity;
	float targetScale;
	float smoothedScale;
	float targetAlpha;
	float smoothedAlpha;
	AxisIndicator axisIndicator;
	AxisIndicator otherAxisIndicator; // used for 2 axes

	Vector3 dragStartPos;
	float distance;
	Vector2 mouseOffset;

	void Awake()
	{
		main = GetComponentInParent<TransformTools>();
		parentMain = GetComponentInParent<TranslateMain>();

		objectRenderer = GetComponent<Renderer>();
		mat = objectRenderer.material;
		color = mat.color;

		targetIntensity = main.defaultIntensity;
		smoothedIntensity = main.defaultIntensity;
		targetAlpha = main.defaultAlpha;
		targetScale = 1f;

		numaxes = axes.x + axes.y + axes.z;
	}

	void Update()
	{
		if (axes != Vector3.one)
			localAxes = main.transform.rotation * axes;

		over = MouseOver();
		mouseDown = main.controls.Transform.Drag.IsPressed();

		if (mouseDown && over && Time.time - lastMouseDownTime < main.doubleClickResetMaxTime && mouseDown != lastMouseDown)
			ResetTransform();
		if (mouseDown != lastMouseDown && !mouseDown) resetting = false;

		bool specialAfterReleaseCase = main.hovering != lastMainHovering;
		if ((over != lastOver || specialAfterReleaseCase) && over)
			StartOver();
		else if (over != lastOver && !over)
			StopOver();

		if (mouseDown != lastMouseDown && mouseDown && !resetting)
			StartClicking();
		else if (mouseDown != lastMouseDown && !mouseDown)
			StopClicking();

		UpdateVisuals();

		PerformDragging();

		UseAxisIndicator();

		if (mouseDown != lastMouseDown && mouseDown) lastMouseDownTime = Time.time;
		lastOver = over;
		lastMouseDown = mouseDown;
		lastMainHovering = main.hovering;
	}

	void ResetTransform() // reset ALL transforms (except position)
	{
		resetting = true;
		foreach (Transform t in main.buildingManager.SelectionManager.selection)
		{
			t.rotation = Quaternion.identity;
			t.localScale = Vector3.one;
		}
		main.buildingManager.SelectionManager.UpdateContainer();
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
		if (!main.hovering && !dragging)// || axes == Vector3.one)
		{
			main.currentlyUsingTransformObj = this;

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
			if (numaxes < 3) axisIndicator = main.axisIndicatorManager.NewIndicator(); // full doesn't need
			if (numaxes == 2) otherAxisIndicator = main.axisIndicatorManager.NewIndicator(); // two axes needs another

			mouseOffset = main.controls.Transform.MousePos.ReadValue<Vector2>() - (Vector2)Camera.main.WorldToScreenPoint(transform.position);
			distance = Vector3.Distance(transform.position, Camera.main.transform.position);
			dragStartPos = main.transform.position;
		}
	}
	public void StopClicking()
	{
		if (!dragging) return;

		main.UpdatePosition();
		transform.localPosition = axes == Vector3.one ? Vector3.zero : axes;

		main.axisIndicatorManager.DestroyIndicator(axisIndicator);
		if (otherAxisIndicator != null) main.axisIndicatorManager.DestroyIndicator(otherAxisIndicator);

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

		mat.SetColor("_EmissiveColor", HF.MultiplyColorByVector(smoothedIntensity, color));
		mat.color = new(color.r, color.g, color.b, smoothedAlpha);
		//mat.SetFloat("_Alpha", smoothedAlpha);
		transform.localScale = smoothedScale * Vector3.one;
	}
	void PerformDragging()
	{
		if (!dragging) return;

		Camera mainCamera = Camera.main;

		Vector3 mouseScreenSpace = main.controls.Transform.MousePos.ReadValue<Vector2>() - mouseOffset;
		mouseScreenSpace.z = mainCamera.nearClipPlane;

		Vector3 cameraPos = mainCamera.transform.position;
		Vector3 cameraVec = mainCamera.ScreenToWorldPoint(mouseScreenSpace) - cameraPos;

		switch (numaxes)
		{
			case 1:
				transform.position = HF.ClosestPointAOnTwoLines(
					dragStartPos, localAxes.normalized,
					cameraPos, cameraVec.normalized); //alot of hacky stuff going on here that i dont understand
				break;
			case 2:
				transform.position = HF.RayPlaneIntersect(
					dragStartPos, main.transform.rotation * (Vector3.one - axes),
					cameraPos, cameraVec);
				break;
			case 3:
				mouseScreenSpace.z = distance;
				transform.position = mainCamera.ScreenToWorldPoint(mouseScreenSpace);
				
				if (main.snapping && main.local)
					transform.position -= dragStartPos;

				break;
		};

		// move target with direction
		main.selectionContainer.position = transform.position - (axes == Vector3.one ? Vector3.zero : localAxes * main.transform.localScale.x);
	
		if (main.snapping)
		{
			switch (numaxes)
			{
				case 1:
					float distanceAlongAxis = HF.DistanceInDirection(main.selectionContainer.position, dragStartPos, localAxes);
					float snappedDist = Mathf.Round(distanceAlongAxis / main.translateSnappingIncrement) * main.translateSnappingIncrement;
					main.selectionContainer.position = dragStartPos + localAxes * snappedDist;
					break;
				case 2:
					Vector3 planeXVector = Vector3.zero;
					Vector3 planeYVector = Vector3.zero;

					if (axes.x != 0)
						planeXVector = main.transform.rotation * Vector3.right;
					if (axes.y != 0)
					{
						if (planeXVector == Vector3.zero)
							planeXVector = main.transform.rotation * Vector3.up;
						else
							planeYVector = main.transform.rotation * Vector3.up;
					}
					if (axes.z != 0)
						planeYVector = main.transform.rotation * Vector3.forward;

					Vector2 planePoint = HF.CoordinatesOfPointOnPlane(main.selectionContainer.position, dragStartPos, planeXVector, planeYVector);
					Vector2 snappedPoint = HF.Vector2Round(planePoint / main.translateSnappingIncrement) * main.translateSnappingIncrement;
					Vector3 worldSpacePoint = dragStartPos + planeXVector * snappedPoint.x + planeYVector * snappedPoint.y;

					main.selectionContainer.position = worldSpacePoint;

					break;
				case 3:
					Vector3 snappedPos = main.local ?
						  (transform.rotation * (HF.Vector3Round(Quaternion.Inverse(transform.rotation) * main.selectionContainer.position / main.translateSnappingIncrement) * main.translateSnappingIncrement))
						: (HF.Vector3Round(main.selectionContainer.position / main.translateSnappingIncrement) * main.translateSnappingIncrement);

					main.selectionContainer.position = snappedPos + (main.local ? dragStartPos : Vector3.zero);
					if(main.local) transform.position += dragStartPos;
					break;
			}
		}
	}
	void UseAxisIndicator()
	{
		if (!dragging) return;
		switch(numaxes)
		{
			case 1:
				axisIndicator.UpdateIndicator(
					(transform.position + dragStartPos) / 2,
					Quaternion.LookRotation(localAxes, transform.up),
					color,
					(transform.position - dragStartPos).magnitude * 2f + main.axisIndicatorLengthOffset);
				break;
			case 2:
				Vector3 axis0 =
					axes == new Vector3(1, 1, 0) ?	new Vector3(1, 0, 0) : (
					axes == new Vector3(0, 1, 1) ?	new Vector3(0, 1, 0) :
										/*1,0,1*/	new Vector3(1, 0, 0));
				Vector3 axis1 = axes - axis0;

				axis0 = main.transform.rotation * axis0;
				axis1 = main.transform.rotation * axis1;

				axisIndicator.UpdateIndicator(
					dragStartPos,
					main.selectionContainer.rotation * Quaternion.LookRotation(axis0, transform.up),
					parentMain.colorOfAxes[axis0],
					(transform.position - dragStartPos).magnitude * 2f + main.axisIndicatorLengthOffset);

				otherAxisIndicator.UpdateIndicator(
					dragStartPos,
					main.selectionContainer.rotation * Quaternion.LookRotation(axis1, transform.up),
					parentMain.colorOfAxes[axis1],
					(transform.position - dragStartPos).magnitude * 2f + main.axisIndicatorLengthOffset);

				break;
		}
	}
}