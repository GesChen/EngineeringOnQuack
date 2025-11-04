using System.Collections.Generic;
using UnityEngine;

public class TransformTools : Singleton<TransformTools> {
	public bool active;
	[Space]
	public WindowManager windowManager;
	public Transform selectionContainer;
	public BuildingManager buildingManager;
	[Space]
	public bool local;
	public float size;
	float currentSize;
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
	public AxisIndicatorManager axisIndicatorManager;
	public float axisIndicatorAlpha;
	public float axisIndicatorLengthOffset;

	[Header("Debug")]
	// this is really old code so i dont know what i was doing here
	// they shoulda been put until a transformtools class but im too lazy to refactor it all now
	//public dynamic currentlyUsingTransformObj;
	public bool hovering;
	public List<Transform> hoveringTransforms;
	public bool dragging;
	public bool specialCenterCase;
	public bool snapping;
	public bool aligning;
	[Space]
	public bool translating;
	public bool rotating;
	public bool scaling;

	void Start() {
		SubscribeToControls();
		SubscribeToBottomBar();
	}

	void Update() {
		// dont display while selecting, issues pop up with interference in hovering and stuff
		bool selectionDragging = SelectionManager.Instance.selectionBoxDragging &&
			(Time.time - SelectionManager.Instance.dragStartTime > Config.Input.clickMaxTimeMs / 1000f);

		currentSize = (active && !selectionDragging) ? size : 0;

		if (!dragging) {
			float dist = HF.DistanceInDirection(
				Camera.main.transform.position,
				selectionContainer.position,
				-Camera.main.transform.forward);

			transform.localScale = dist * currentSize * Vector3.one;
		}
		if (local && !dragging)
			transform.rotation = selectionContainer.rotation;
		else if (!local)
			transform.rotation = Quaternion.identity;

		snapping = Conatrols.IM.Building.Snap.IsPressed();

		aligning = Conatrols.IM.Building.Align.IsPressed();

		// may change later
		if (aligning) snapping = false;
	}
	public void UpdatePosition() {
		transform.position = selectionContainer.position;
	}

	void SubscribeToControls() {
		TransformToolsMenu.ClearEvents();

		TransformToolsMenu.onTranslatePressed += ToggleTranslate;
		TransformToolsMenu.onRotatePressed += ToggleRotate;
		TransformToolsMenu.onScalePressed += ToggleScale;
	}

	void SubscribeToBottomBar() {
		BottomBar.ClearTransform();
		BottomBar.OnTransformOpened += () => SetUIState(true);
	}

	public void SetUIState(bool state) {
		if (state)
			TransformToolsMenu.MainWindow.RealisedWindow.Show();
		else
			TransformToolsMenu.MainWindow.RealisedWindow.Hide();
	}

	void ToggleTranslate() => translating = !translating;
	void ToggleRotate() => rotating = !rotating;
	void ToggleScale() => scaling = !scaling;
}