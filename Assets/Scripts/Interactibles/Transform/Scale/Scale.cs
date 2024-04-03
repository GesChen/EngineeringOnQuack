using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

public class Scale : MonoBehaviour
{
	public Vector3 axis;
	public bool full;
	[HideInInspector] public Vector3 localAxes;

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
	float lastMouseDownTime;
	bool resetting;

	Color color;

	Vector3 targetIntensity;
	Vector3 smoothedIntensity;
	float targetScale;
	float smoothedScale;
	float targetAlpha;
	float smoothedAlpha;

	Vector3 dragStartPos;
	Vector2 dragStartMousePos;
	Vector2 dragStartSSPos;
	Vector2 mouseOffset;
	Vector3 startScale;

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
		localAxes = main.transform.rotation * axis;

		over = MouseOver() && !main.specialCenterCase;
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
		Vector3 scaling = main.target.transform.localScale;
		scaling.Scale(Vector3.one - axis);
		scaling += axis;
		if (full) scaling = Vector3.one;
		main.target.transform.localScale = scaling;
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

			dragStartMousePos = main.controls.Transform.MousePos.ReadValue<Vector2>();
			dragStartSSPos = Camera.main.WorldToScreenPoint(transform.position);
			mouseOffset = dragStartMousePos - (Vector2)Camera.main.WorldToScreenPoint(transform.position);
			dragStartPos = main.transform.position;
			startScale = main.target.localScale;
		}
	}
	void StopClicking()
	{
		if (!dragging) return;

		main.transform.position = main.target.position;

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

		mat.SetColor("_EmissiveColor", HelperFunctions.MultiplyColorByVector(smoothedIntensity, color));
		mat.color = new(color.r, color.g, color.b, smoothedAlpha);
		//mat.SetFloat("_Alpha", smoothedAlpha);
		transform.localScale = smoothedScale * Vector3.one;
	}
	void PerformScalingAxis()
	{
		if (!dragging) return;

		Vector3 mouseScreenSpace = main.controls.Transform.MousePos.ReadValue<Vector2>() - mouseOffset;
		mouseScreenSpace.z = Camera.main.nearClipPlane;

		Vector3 cameraPos = Camera.main.transform.position;
		Vector3 cameraVec = Camera.main.ScreenToWorldPoint(mouseScreenSpace) - cameraPos;

		Vector3 newPos = HelperFunctions.ClosestPointAOnTwoLines(
			dragStartPos, localAxes.normalized,
			cameraPos, cameraVec.normalized);

		transform.position = newPos;
		float distance = transform.localPosition.magnitude;
		float offset = ((main.translating || main.rotating) ? main.scaleAxesDistWOthers : main.scaleAxesDistDefault) - 1;

		Vector3 keep = HelperFunctions.MV3(Vector3.one - axis, startScale);
		Vector3 newScale = (distance * (1/(1+offset)) - 1) * axis;
		main.target.transform.localScale = HelperFunctions.MV3(startScale, Vector3.one + newScale);
	}
	void PerformScalingFull()
	{
		if (!dragging) return;
		
		Vector2 mouseScreenSpace = main.controls.Transform.MousePos.ReadValue<Vector2>();

		Debug.Log(dragStartMousePos);
		Debug.Log(mouseScreenSpace);
		Debug.Log(dragStartSSPos);

		float scale = (mouseScreenSpace - dragStartSSPos).magnitude / (dragStartSSPos - dragStartMousePos).magnitude;

		main.target.localScale = startScale * scale;
	}
	void UseAxisIndicator()
	{
		if (dragging && !full)
		{
			main.axisIndicator.inUse = true;
			main.axisIndicator.color = color;
			main.axisIndicator.transform.position = (main.target.position + dragStartPos) / 2;
			main.axisIndicator.transform.localScale = new(.015f, .015f, (transform.position - dragStartPos).magnitude * 2f + main.axisIndicatorLengthOffset);
			main.axisIndicator.rotation = main.target.rotation * Quaternion.LookRotation(axis, transform.up);
		}
	}
}