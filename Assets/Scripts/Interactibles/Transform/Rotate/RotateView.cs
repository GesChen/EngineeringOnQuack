using PlasticPipe.PlasticProtocol.Messages;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class RotateView : MonoBehaviour
{ 
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
	float lastMouseDownTime;

	float targetIntensity;
	float smoothedIntensity;
	float targetOutset;
	float smoothedOutset;
	float targetAlpha;
	float smoothedAlpha;

	bool firstFrameAfterStartDrag;
	float angleOffset;
	Quaternion targetStartRotation;

	void Awake()
	{
		main = GetComponentInParent<TransformTools>();
		mat = GetComponent<MeshRenderer>().material;
		color = mat.color;

		targetIntensity = main.defaultWhiteIntensity;
		targetOutset = main.defaultOutset;
		targetAlpha = main.defaultAlpha;

		samplePoints = transform.Find("sample points").GetComponentsInChildren<Transform>();
		samplePoints = samplePoints[1..];
	}

	void Update()
	{
		over = MouseOver();
		mouseDown = main.controls.Transform.Drag.IsPressed();

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
		main.target.transform.rotation = Quaternion.identity;
	}
	bool MouseOver()
	{
		// get screen point positions of all sample points
		Vector2[] screenPointPositions = new Vector2[18];
		for (int i = 0; i < 18; i++)
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

			targetIntensity = main.hoverWhiteIntensity;
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

			targetIntensity = main.defaultWhiteIntensity;
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

			targetIntensity = main.draggingWhiteIntensity;
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
			targetIntensity = main.hoverWhiteIntensity;
			targetOutset = main.hoverOutset;
		}
		else
		{
			hovering = false;
			main.hovering = false;
			targetIntensity = main.defaultWhiteIntensity;
			targetOutset = main.defaultOutset;
		}
	}
	void UpdateVisuals()
	{
		if (main.dragging && !dragging) targetAlpha = main.draggingAlpha;
		else if (main.hovering && !hovering) targetAlpha = main.notHoveredAlpha;
		else targetAlpha = main.defaultAlpha;
		
		// smoothing, can use different fucntions
		smoothedIntensity = Mathf.Lerp(smoothedIntensity, targetIntensity, main.intensitySmoothness);
		smoothedOutset = Mathf.Lerp(smoothedOutset, targetOutset, main.scaleSmoothness);
		smoothedAlpha = Mathf.Lerp(smoothedAlpha, targetAlpha, main.alphaSmoothness);

		transform.rotation = Camera.main.transform.rotation;
		// rotation uses a different emission system
		mat.SetColor("_Color", color * smoothedIntensity);
		mat.SetFloat("_Alpha", smoothedAlpha);
		mat.SetFloat("_VertexOffset", smoothedOutset);
	}
	void PerformRotating()
	{
		if (!dragging) return;

		Vector2 mousePos = main.controls.Transform.MousePos.ReadValue<Vector2>();

		Vector3 normal = Camera.main.transform.rotation * Vector3.forward;

		float angle = -Vector2.SignedAngle(mousePos - (Vector2) Camera.main.WorldToScreenPoint(transform.position), Vector2.right);

		if (firstFrameAfterStartDrag)
		{
			targetStartRotation = main.target.rotation;
			angleOffset = angle;
		}

		main.target.rotation = targetStartRotation * Quaternion.AngleAxis(angle - angleOffset, Quaternion.Inverse(targetStartRotation) * normal);
		firstFrameAfterStartDrag = false;
	}
}
