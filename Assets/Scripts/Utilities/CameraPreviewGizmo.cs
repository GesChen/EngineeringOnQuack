using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraPreviewGizmo : MonoBehaviour {
	[Range(0.1f, 100f)]
	public float previewLength = 5f;

	[Tooltip("Length of the up-tick line")]
	public float upTickSize = 0.2f;

	private Camera cam;

	private void OnDrawGizmos() {
		if (cam == null)
			cam = GetComponent<Camera>();

		// Get camera basis
		Transform t = cam.transform;
		Vector3 origin = t.position;
		Vector3 forward = t.forward;
		Vector3 right = t.right;
		Vector3 up = t.up;

		// Frustum size at preview length
		float height = 2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * previewLength;
		float width = height * cam.aspect;

		Vector3 center = origin + forward * previewLength;

		// Calculate corners
		Vector3 topLeft     = center + (up * height / 2) - (right * width / 2);
		Vector3 topRight    = center + (up * height / 2) + (right * width / 2);
		Vector3 bottomLeft  = center - (up * height / 2) - (right * width / 2);
		Vector3 bottomRight = center - (up * height / 2) + (right * width / 2);

		Gizmos.color = Color.white;

		// Draw pyramid lines
		Gizmos.DrawLine(origin, topLeft);
		Gizmos.DrawLine(origin, topRight);
		Gizmos.DrawLine(origin, bottomLeft);
		Gizmos.DrawLine(origin, bottomRight);

		// Draw rectangle at preview plane
		Gizmos.DrawLine(topLeft, topRight);
		Gizmos.DrawLine(topRight, bottomRight);
		Gizmos.DrawLine(bottomRight, bottomLeft);
		Gizmos.DrawLine(bottomLeft, topLeft);

		// Draw up tick at top center
		Vector3 topCenter = center + up * (height / 2f);
		Vector3 upTickEnd = topCenter + up * upTickSize;
		Gizmos.DrawLine(topCenter, upTickEnd);
	}
}
