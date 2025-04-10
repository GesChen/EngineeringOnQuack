#define DEBUGMODE

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class RotateAxis : MonoBehaviour
{ 
	public Vector3 axis;
	public float distance;
	public Transform snappingIndicatorsParent;
	public Material snappingIndicatorMaterial;

	private TransformTools main;
	private Material mat;
	private Color color;

	private Transform[] samplePoints;
	
	bool over;
	bool lastOver;
	bool mouseDown;
	bool lastMouseDown;
	bool lastMainHovering;
	bool hovering;
	bool dragging;
	float lastMouseDownTime;

	Vector3 targetIntensity;
	Vector3 smoothedIntensity;
	float targetOutset;
	float smoothedOutset;
	float targetAlpha;
	float smoothedAlpha;
	AxisIndicator axisIndicator;

	bool firstFrameAfterStartDrag;
	float startAngle;
	Quaternion targetStartRotation;

	void Awake()
	{
		main = GetComponentInParent<TransformTools>();
		mat = GetComponent<MeshRenderer>().material;
		color = mat.color;

		targetIntensity = main.defaultIntensity;
		targetOutset = main.defaultOutset;
		targetAlpha = main.defaultAlpha;

		samplePoints = transform.Find("sample points").GetComponentsInChildren<Transform>();
		samplePoints = samplePoints[1..];
	}

	void Update()
	{
		over = MouseOver();
		mouseDown = Controls.IM.Transform.Drag.IsPressed();

		if (mouseDown && over && Time.time - lastMouseDownTime < main.doubleClickResetMaxTime && mouseDown != lastMouseDown)
			ResetTransform();

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

		PerformRotating();

		if (mouseDown != lastMouseDown && mouseDown) lastMouseDownTime = Time.time;
		lastOver = over;
		lastMouseDown = mouseDown;
		lastMainHovering = main.hovering;
	}
	void ResetTransform()
	{
		foreach (Transform t in SelectionManager.Instance.selection)
		{
			Vector3 rotation = t.rotation.eulerAngles;
			rotation.Scale(Vector3.one - axis);
			t.rotation = Quaternion.Euler(rotation);
		}
		SelectionManager.Instance.UpdateContainer();
	}
	bool MouseOver()
	{
		// get screen point positions of all sample points
		Vector2[] screenPointPositions = new Vector2[36];
		for (int i = 0; i < 36; i++)
			screenPointPositions[i] = Camera.main.WorldToScreenPoint(samplePoints[i].position);

		Vector2 mousePos = Controls.IM.Transform.MousePos.ReadValue<Vector2>();
		float mouseToCircleDistance = HF.PointToPolygonEdgeDistance(mousePos, screenPointPositions);
		return mouseToCircleDistance <= distance;
	}
	void StartOver()
	{
		if (!main.hovering && !dragging)
		{
			main.currentlyUsingTransformObj = this;
			hovering = true;
			main.hovering = true;

			targetIntensity = main.hoverIntensity;
			targetOutset = main.hoverOutset;
			mat.SetFloat("_TransparentSortPriority", 1);
			HDMaterial.ValidateMaterial(mat);
		}
	}
	public void StopOver()
	{
		if (hovering && !dragging)
		{
			hovering = false;
			main.hovering = false;

			targetIntensity = main.defaultIntensity;
			targetOutset = main.defaultOutset;
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
			targetOutset = main.draggingOutset;

			// handle axis indicator
			axisIndicator = main.axisIndicatorManager.NewIndicator();
			axisIndicator.UpdateIndicator(
				transform.position,
				main.transform.rotation * Quaternion.LookRotation(axis, Vector3.up),
				mat.color);
			
			/*/ handle rotate snap indicator
			main.rotateSnapIndicator.inUse = true;
			main.rotateSnapIndicator.parent = snappingIndicatorsParent;
			main.rotateSnapIndicator.material = snappingIndicatorMaterial;
			*/
			firstFrameAfterStartDrag = true;
		}
	}
	public void StopClicking()
	{
		if (!dragging) return;

		main.axisIndicatorManager.DestroyIndicator(axisIndicator);
		//main.rotateSnapIndicator.inUse = false;
		dragging = false;
		main.dragging = false;
		if (over)
		{
			hovering = true;
			main.hovering = true;
			targetIntensity = main.hoverIntensity;
			targetOutset = main.hoverOutset;
		}
		else
		{
			hovering = false;
			main.hovering = false;
			targetIntensity = main.defaultIntensity;
			targetOutset = main.defaultOutset;
		}
	}
	void UpdateVisuals()
	{
		if (main.dragging && !dragging) targetAlpha = main.draggingAlpha;
		else if (main.hovering && !hovering) targetAlpha = main.notHoveredAlpha;
		else targetAlpha = main.defaultAlpha;
		
		// smoothing, can use different fucntions
		smoothedIntensity = Vector3.Lerp(smoothedIntensity, targetIntensity, main.intensitySmoothness);
		smoothedOutset = Mathf.Lerp(smoothedOutset, targetOutset, main.scaleSmoothness);
		smoothedAlpha = Mathf.Lerp(smoothedAlpha, targetAlpha, main.alphaSmoothness);

		// rotation uses a different emission system
		mat.SetColor("_Color", HF.MultiplyColorByVector(smoothedIntensity, color));
		mat.SetFloat("_Alpha", smoothedAlpha);
		mat.SetFloat("_VertexOffset", smoothedOutset);
	}

	void PerformRotating()
	{
		if (!dragging) return;

		Vector3 planePos = transform.position;
		Vector3 planeNormal = (main.transform.rotation * axis).normalized;

		Vector3 mouseScreenSpace = Controls.IM.Transform.MousePos.ReadValue<Vector2>();
		mouseScreenSpace.z = Camera.main.nearClipPlane;

		Vector3 cameraPos = Camera.main.transform.position;
		Vector3 cameraVec = Camera.main.ScreenToWorldPoint(mouseScreenSpace) - cameraPos;

		Quaternion planeRotation = Quaternion.LookRotation(planeNormal);

#if DEBUGMODE
		Debug.DrawRay(planePos, planeNormal, Color.blue);
		Debug.DrawRay(planePos, planeRotation * Vector3.right * 5, Color.red);
		Debug.DrawRay(planePos, planeRotation * Vector3.left  * 5, Color.red);
		Debug.DrawRay(planePos, planeRotation * Vector3.up    * 5, Color.green);
		Debug.DrawRay(planePos, planeRotation * Vector3.down  * 5, Color.green);

		Debug.DrawRay(cameraPos, cameraVec * 50, Color.white, 0, true);
#endif

		Vector3 planeHitPos = HF.RayPlaneIntersect(
			planePos, planeNormal, cameraPos, cameraVec);

#if DEBUGMODE
		DebugExtra.DrawEmpty(planeHitPos, .1f);
#endif

		Vector2 localPosPointOnPlane = HF.CoordinatesOfPointOnPlane(planeHitPos, planePos, planeRotation * Vector3.right, planeRotation * Vector3.up);
		float angle = Mathf.Rad2Deg * Mathf.Atan2(localPosPointOnPlane.y, localPosPointOnPlane.x);

		if (firstFrameAfterStartDrag)
		{
			startAngle = angle;
			targetStartRotation = main.selectionContainer.rotation;
			//main.rotateSnapIndicator.startAngle = startAngle;
		}

		float angleDelta = angle - startAngle;
		if (main.snapping)
			angleDelta = Mathf.Round(angleDelta / main.rotateSnappingIncrement) * main.rotateSnappingIncrement;
			/*if (main.local)
			else
			{
				Vector2 startRotProjectionOnPlane = HelperFunctions.ProjectPointOntoPlane(targetStartRotation * Vector3.forward, planePos, planeNormal);
				float startRotAngleInAxis = Mathf.Rad2Deg * Mathf.Atan2(startRotProjectionOnPlane.y, startRotProjectionOnPlane.x);
				DebugExtra.DrawArrow(planePos, targetStartRotation * Vector3.forward);
				Debug.Log($"pp {startRotProjectionOnPlane}");
				Debug.Log($"sa {startRotAngleInAxis}");
				angleDelta = Mathf.Round(angle / main.rotateSnappingIncrement) * main.rotateSnappingIncrement;
			}*/

#if DEBUGMODE
		DebugExtra.DrawPoint(planeHitPos - planePos, Color.blue); 
#endif

		transform.localRotation = Quaternion.AngleAxis(angleDelta, axis);
		main.selectionContainer.rotation = targetStartRotation * Quaternion.AngleAxis(angleDelta, Quaternion.Inverse(targetStartRotation) * planeNormal);
		
		firstFrameAfterStartDrag = false;
	}
}
