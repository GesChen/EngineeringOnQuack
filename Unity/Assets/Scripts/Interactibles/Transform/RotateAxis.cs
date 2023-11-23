using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class RotateAxis : MonoBehaviour
{ 
	public Vector3 axis;
	public float distance;

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

	Vector3 targetIntensity;
	Vector3 smoothedIntensity;
	float targetOutset;
	float smoothedOutset;
	float targetAlpha;
	float smoothedAlpha;

	bool firstFrameAfterStartDrag;
	Vector3 arbitrarySecondaryVectorOnPlane;
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
		AxisIndicator();

		PerformRotating();

		lastOver = over;
		lastMouseDown = mouseDown;
		lastMainHovering = main.hovering;
	}
	bool MouseOver()
	{
		// get screen point positions of all sample points
		Vector2[] screenPointPositions = new Vector2[36];
		for (int i = 0; i < 36; i++)
			screenPointPositions[i] = Camera.main.WorldToScreenPoint(samplePoints[i].position);

		Vector2 mousePos = main.controls.Transform.MousePos.ReadValue<Vector2>();
		float mouseToCircleDistance = HelperFunctions.PointToPolygonEdgeDistance(mousePos, screenPointPositions);
		return mouseToCircleDistance <= distance;
	}
	void StartOver()
	{
		if (!main.hovering && !dragging)
		{
			hovering = true;
			main.hovering = true;

			targetIntensity = main.hoverIntensity;
			targetOutset = main.hoverOutset;
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

			firstFrameAfterStartDrag = true;
		}
	}
	void StopClicking()
	{
		if (!dragging) return;
		
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
		mat.SetColor("_Color", TransformTools.MultiplyColorByVector(smoothedIntensity, color));
		mat.SetFloat("_Alpha", smoothedAlpha);
		mat.SetFloat("_VertexOffset", smoothedOutset);
	}
	void AxisIndicator()
	{
		if (dragging)
		{
			main.axisIndicator.rotation = Quaternion.LookRotation(axis, transform.forward);
		}
	}
	void PerformRotating()
	{
		if (!dragging) return;

		Vector3 planePos = transform.position;
		Vector3 planeNormal = (main.transform.rotation * axis).normalized;

		Vector3 mouseScreenSpace = main.controls.Transform.MousePos.ReadValue<Vector2>();
		mouseScreenSpace.z = Camera.main.nearClipPlane;

		Vector3 cameraPos = Camera.main.transform.position;
		Vector3 cameraVec = Camera.main.ScreenToWorldPoint(mouseScreenSpace) - cameraPos;

		Quaternion planeRotation = Quaternion.LookRotation(planeNormal);
		Debug.DrawRay(planePos, planeNormal, Color.blue);
		Debug.DrawRay(planePos, planeRotation * Vector3.right * 5, Color.red);
		Debug.DrawRay(planePos, planeRotation * Vector3.left  * 5, Color.red);
		Debug.DrawRay(planePos, planeRotation * Vector3.up    * 5, Color.green);
		Debug.DrawRay(planePos, planeRotation * Vector3.down  * 5, Color.green);

		Debug.DrawRay(cameraPos, cameraVec * 50, Color.white, 0, true);

		Vector3 planeHitPos = TransformTools.RayPlaneIntersect(
			planePos, planeNormal, cameraPos, cameraVec);

		DebugExtra.DrawEmpty(planeHitPos, .1f);

		if (firstFrameAfterStartDrag)
		{
			targetStartRotation = main.target.rotation;
			arbitrarySecondaryVectorOnPlane = (planePos - planeHitPos).normalized;
		}
		float angle = 180 + Vector3.SignedAngle(arbitrarySecondaryVectorOnPlane - planePos, planeHitPos - planePos, planeNormal);

		transform.localRotation = Quaternion.AngleAxis(angle, axis);
		main.target.rotation = targetStartRotation * Quaternion.AngleAxis(angle, Quaternion.Inverse(targetStartRotation) * planeNormal);

		firstFrameAfterStartDrag = false;
	}
}
