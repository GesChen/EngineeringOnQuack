//#define DEBUGMODE

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class Scale : MonoBehaviour
{
	public Vector3 axis;
	public bool full;
	[HideInInspector] public Vector3 localAxes;

	private TransformTools main;
	private Material mat;
	private Renderer objectRenderer;
	private MeshFilter meshFilter;

	bool over;
	bool lastOver;
	bool mouseDown;
	bool lastMouseDown;
	bool lastMainHovering;
	bool hovering;
	bool dragging;
	float lastMouseDownTime;
	bool resetting;

	Color color;

	Vector3 targetIntensity;
	Vector3 smoothedIntensity;
	float targetScale;
	float smoothedScale;
	float targetAlpha;
	float smoothedAlpha;
	List<AxisIndicator> axisIndicators = new();
	Dictionary<Transform, AxisIndicator> objectAxisIndicators = new();
	List<Transform> lastSelection = new();

	Vector3 dragStartPos;
	Vector2 dragStartMousePos;
	Vector2 dragStartSSPos;
	Vector2 mouseOffset;
	Dictionary<Transform,Vector3> startScales;

	void Awake()
	{
		main = GetComponentInParent<TransformTools>();
		objectRenderer = GetComponent<Renderer>();
		meshFilter = GetComponent<MeshFilter>();
		mat = objectRenderer.material;
		color = mat.color;

		targetIntensity = main.defaultIntensity;
		smoothedIntensity = main.defaultIntensity;
		targetAlpha = main.defaultAlpha;
		targetScale = 1f;
	}

	void Update()
	{
		localAxes = main.transform.rotation * axis;

		over = MouseOver() && !main.specialCenterCase;
		mouseDown = Conatrols.IM.Transform.Drag.IsPressed();

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

		if (full)
			PerformScalingFull();
		else
			PerformScalingAxis();
		
		UseAxisIndicator();

		if (mouseDown != lastMouseDown && mouseDown) lastMouseDownTime = Time.time;
		lastOver = over;
		lastMouseDown = mouseDown;
		lastMainHovering = main.hovering;
	}
	void ResetTransform()
	{
		resetting = true;
		foreach (Transform t in SelectionManager.Instance.selection)
		{
			Vector3 scaling = t.localScale;
			scaling.Scale(Vector3.one - axis);
			scaling += axis;
			if (full) scaling = Vector3.one;
			t.localScale = scaling;
		}
		SelectionManager.Instance.UpdateContainer();
	}
	bool MouseOver()
	{
		/*// get world bounds and camera
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
		*/

		// calculate the bounds of the screen space bounds
		Vector2 minScreen = Vector2.positiveInfinity;
		Vector2 maxScreen = Vector2.negativeInfinity;

		for (int i = 0; i < meshFilter.sharedMesh.vertexCount; i++)
		{
			Vector3 vertexPos = transform.position + (Vector3)(transform.localToWorldMatrix * meshFilter.sharedMesh.vertices[i]);
#if DEBUGMODE
			DebugExtra.DrawPoint(vertexPos, .1f);
#endif
			Vector2 ssPos = Camera.main.WorldToScreenPoint(vertexPos);
			minScreen = Vector2.Min(minScreen, ssPos);
			maxScreen = Vector2.Max(maxScreen, ssPos);
		}

		Vector2 mousePos = Conatrols.Mouse.Position;
		bool inBounds = mousePos.x >= minScreen.x + main.boundsOffset && mousePos.x <= maxScreen.x - main.boundsOffset &&
						mousePos.y >= minScreen.y + main.boundsOffset && mousePos.y <= maxScreen.y - main.boundsOffset;
		return inBounds;
	}
	void StartOver()
	{
		if (!main.hovering && !dragging)
		{
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
			UpdateAxisIndicators();

			dragStartMousePos = Conatrols.Mouse.Position;
			dragStartSSPos = Camera.main.WorldToScreenPoint(transform.position);
			mouseOffset = dragStartMousePos - (Vector2)Camera.main.WorldToScreenPoint(transform.position);
			dragStartPos = main.transform.position;

			startScales = new();
			foreach (Transform t in SelectionManager.Instance.selection)
			{
				startScales[t] = t.localScale;
			}
		}
	}
	void StopClicking()
	{
		if (!dragging) return;

		main.UpdatePosition();

		ResetAxisIndicators();

		// reset dragging logic
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
		
		if (!dragging) 
			transform.localPosition = axis * ((main.translating || main.rotating) ? main.scaleAxesDistWOthers : main.scaleAxesDistDefault);

		mat.SetColor("_EmissiveColor", HF.MultiplyColorByVector(smoothedIntensity, color));
		mat.color = new(color.r, color.g, color.b, smoothedAlpha);
		//mat.SetFloat("_Alpha", smoothedAlpha);
		transform.localScale = smoothedScale * Vector3.one;
	}
	void PerformScalingAxis()
	{
		if (!dragging) return;

		Vector3 mouseScreenSpace = Conatrols.Mouse.Position - mouseOffset;
		mouseScreenSpace.z = Camera.main.nearClipPlane;

		Vector3 cameraPos = Camera.main.transform.position;
		Vector3 cameraVec = Camera.main.ScreenToWorldPoint(mouseScreenSpace) - cameraPos;

		Vector3 newPos = HF.ClosestPointAOnTwoLines(
			dragStartPos, localAxes.normalized,
			cameraPos, cameraVec.normalized);

		transform.position = newPos;
		float distance = transform.localPosition.magnitude;
		float offset = ((main.translating || main.rotating) ? main.scaleAxesDistWOthers : main.scaleAxesDistDefault) - 1;

		float scaleInAxis = distance - offset;
		if (scaleInAxis < 1)
			scaleInAxis = distance / (1 + offset);

		if (!main.snapping)
		{
			Vector3 newScale = scaleInAxis * axis + Vector3.one - axis;

			foreach (Transform t in SelectionManager.Instance.selection)
			{
				t.localScale = HF.MV3(startScales[t], newScale);
			}
		}
		else
		{	foreach (Transform t in SelectionManager.Instance.selection)
			{
				float currentScaleInAxis = HF.MV3(startScales[t], axis).magnitude;

				scaleInAxis -= 1;
				if (main.local) scaleInAxis += currentScaleInAxis; // different logic depending on local or not 
				scaleInAxis = Mathf.Round(scaleInAxis / main.scaleSnappingIncrement) * main.scaleSnappingIncrement;
				if (!main.local) scaleInAxis += currentScaleInAxis;

				Vector3 newScale = HF.MV3(startScales[t], Vector3.one - axis) + axis * scaleInAxis;
				t.localScale = newScale;
			}
		}
	}
	void PerformScalingFull()
	{
		if (!dragging) return;
		
		Vector2 mouseScreenSpace = Conatrols.Mouse.Position;

		float scale = (mouseScreenSpace - dragStartSSPos).magnitude / (dragStartSSPos - dragStartMousePos).magnitude;

		if (scale > 1)
			scale = 1 + scale * main.fullScaleFactor;

		if (main.snapping)
			scale = Mathf.Round(scale / main.scaleSnappingIncrement) * main.scaleSnappingIncrement;

		foreach (Transform t in SelectionManager.Instance.selection)
		{
			t.localScale = startScales[t] * scale;
		}
	}
	void UseAxisIndicator()
	{
		if (lastSelection != SelectionManager.Instance.selection) // selection changed, update axis indicator list
		{
			UpdateAxisIndicators();
		}

		if (dragging && !full)
		{
			foreach (Transform t in SelectionManager.Instance.selection)
			{
				objectAxisIndicators[t].UpdateIndicator(
					t.position,
					t.rotation * Quaternion.LookRotation(axis, transform.up),
					color,
					(transform.position - dragStartPos).magnitude * 2f + main.axisIndicatorLengthOffset);
			}
		}

		lastSelection = SelectionManager.Instance.selection;
	}
	void UpdateAxisIndicators()
	{
		if (dragging)
		{
			ResetAxisIndicators();

			axisIndicators = new();
			objectAxisIndicators = new();
			foreach (Transform t in SelectionManager.Instance.selection)
			{
				AxisIndicator axisIndicator = main.axisIndicatorManager.NewIndicator();
				axisIndicators.Add(axisIndicator);
				objectAxisIndicators.Add(t, axisIndicator);
			}
		}
	}
	void ResetAxisIndicators()
	{
		foreach (AxisIndicator axisIndicator in axisIndicators)
		{
			main.axisIndicatorManager.DestroyIndicator(axisIndicator);
		}
		axisIndicators = new();
		objectAxisIndicators = new();
	}
}