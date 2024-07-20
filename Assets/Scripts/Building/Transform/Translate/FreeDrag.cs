using UnityEngine;

public class FreeDrag : MonoBehaviour
{
	public TransformTools main;

	Vector2 mousePos;
	bool mouseDown;
	bool lastMouseDown;
	
	bool dragging;
	Transform draggingObj;
	float draggingPlaneDistance;
	Vector3 draggingObjectOffset;
	Vector3 draggingSlightOffset; // weird thing, just to compensate

	void Update()
	{
		mousePos = Controls.inputMaster.Transform.MousePos.ReadValue<Vector2>();
		mouseDown = Controls.inputMaster.Transform.Drag.IsPressed();

		if (mouseDown != lastMouseDown && mouseDown)
			StartClicking();
		else if (mouseDown != lastMouseDown && !mouseDown)
			StopClicking();
		
		PerformDragging();

		lastMouseDown = mouseDown;

	}
	void StartClicking()
	{
		if (main.hovering || main.dragging) return;

		if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out RaycastHit hit))
		{
			if (hit.transform.GetComponent<Part>()) // is a part, not some random obj
			{
				//main.target = hit.transform;

				dragging = true;
				main.hovering = true;
				main.dragging = true;

				draggingPlaneDistance = hit.distance;
				draggingObjectOffset = hit.point - hit.transform.position;
				draggingObj = hit.transform;
				
				// screen2world doesnt produce same postion as the point; just to compensate
				Vector3 withDistance = mousePos;
				withDistance.z = hit.distance;
				Vector3 convertedPos = Camera.main.ScreenToWorldPoint(withDistance);
				draggingSlightOffset = hit.point - convertedPos;
			}
		}
	}
	void StopClicking()
	{
		dragging = false;
		main.hovering = false;
		main.dragging = false;
	}
	void PerformDragging()
	{
		if (!dragging) return;

		Vector3 withDistance = mousePos;
		withDistance.z = draggingPlaneDistance;
		draggingObj.position = Camera.main.ScreenToWorldPoint(withDistance) - draggingObjectOffset + draggingSlightOffset;
	}
}
