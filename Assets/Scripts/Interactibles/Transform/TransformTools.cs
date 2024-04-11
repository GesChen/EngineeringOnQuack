using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformTools : MonoBehaviour
{
	public Transform target;
	public bool local;
	public float size;
	public float boundsOffset;
	public float maxMouseSpeedToScaleOut;
	public float doubleClickResetMaxTime = .2f;

	[Header("Snapping")]
	public float translateSnappingIncrement = 1f;
	public float rotateSnappingIncrement = 15f;
	public float scaleSnappingIncrement = 1f;
	public RotateSnapIndicator rotateSnapIndicator;

	[Header("Settings")]
	public float intensitySmoothness = .3f;
	public float scaleSmoothness = .3f;
	public float alphaSmoothness = .1f;
	public float moveSmoothness = .3f;

	[Header("Customization")]
	public float scaleAxesDistDefault = 1f;
	public float scaleAxesDistWOthers = 1.4f;
	public float scaleAxesScaleOffsetWithTransform = -.3f;
	public float fullScaleFactor = .01f;

	[Header("Default")]
	public Vector3 defaultIntensity = Vector3.one;
	public float defaultWhiteIntensity = 1f;
	public float defaultOutset = .04f;
	public float defaultDistance = 10f;
	public float defaultAlpha = .85f;

	[Header("On Hover")]
	public Vector3 hoverIntensity = new(2, 3, 5);
	public float hoverWhiteIntensity = 2f;
	public float hoverScale = 1.3f;
	public float hoverOutset = .08f;
	public float hoverDistance = 15f;
	public float notHoveredAlpha = .5f;

	[Header("On Drag")]
	public Vector3 draggingIntensity = new(3, 4, 6);
	public float draggingWhiteIntensity = 3f;
	public float draggingScale = 1.2f;
	public float draggingOutset = .07f;
	public float draggingAlpha = .01f;
	
	[Header ("Axis Indicator")]
	public AxisIndicator axisIndicator;
	public float axisIndicatorAlpha;
	public float axisIndicatorLengthOffset;

	[Header("Debug")]
	public dynamic currentlyUsingTransformObj;
	public bool hovering;
	public List<Transform> hoveringTransforms;
	public bool dragging;
	public bool specialCenterCase;
	public bool snapping;
	[Space]
	public bool translating;
	public bool rotating;
	public bool scaling;

	[HideInInspector] public InputMaster controls;

	void Awake()
	{
		controls = new InputMaster();
	}
	void OnEnable()
	{
		controls ??= new InputMaster();
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
		
		if (local && !dragging)
			transform.rotation = target.rotation;
		else if (!local)
			transform.rotation = Quaternion.identity;

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			currentlyUsingTransformObj.StopOver();
			hovering = false;
		}

		snapping = controls.Transform.Snap.IsPressed();
	}
}
