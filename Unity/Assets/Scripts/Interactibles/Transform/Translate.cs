using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Translate : MonoBehaviour
{
	public Vector3 axes;

	private TranslateMain main;
	private Material mat;
	private Renderer objectRenderer;

	Color color;
	InputMaster controls;

	float targetIntensity;
	float smoothedIntensity;

	float targetScale;
	float smoothedScale;

    void Awake()
    {
        main = GetComponentInParent<TranslateMain>();
        controls = new InputMaster();
        objectRenderer = GetComponent<Renderer>();
        mat = objectRenderer.material;
		color = mat.color;

        targetIntensity = main.defaultIntensity;
        smoothedIntensity = main.defaultIntensity;

        targetScale = 1f;
        smoothedScale = 1f;
    }

    void OnEnable()
    {
        if (controls == null)
            controls = new InputMaster();
        controls.Enable();
    }
    void OnDisable()
    {
        controls.Disable();
    }


	void Update()
	{
		// get world bounds and camera
		Bounds bounds = objectRenderer.bounds;
		Camera mainCamera = Camera.main; 

		// calculate screen point positions of those bounds
		Vector3[] boundsCorners = new Vector3[8];
		boundsCorners[0] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3( 1,   1,   1)));
		boundsCorners[1] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1,   1,   1)));
		boundsCorners[2] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3( 1,  -1,   1)));
		boundsCorners[3] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3( 1,   1,  -1)));
		boundsCorners[4] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1,  -1,   1)));
		boundsCorners[5] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3( 1,  -1,  -1)));
		boundsCorners[6] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1,   1,  -1)));
		boundsCorners[7] = mainCamera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1,  -1,  -1)));

		// calculate the bounds of the screen space bounds
		Vector2 minScreen = Vector2.positiveInfinity;
		Vector2 maxScreen = Vector2.negativeInfinity;

		foreach (Vector2 corner in boundsCorners) { 
			minScreen = Vector2.Min(minScreen, corner);
			maxScreen = Vector2.Max(maxScreen, corner);
		}

		// determine if mouse is inside ss bounds
		Vector2 mousePos = controls.Transform.MousePos.ReadValue<Vector2>();
		bool inBounds = mousePos.x >= minScreen.x && mousePos.x <= maxScreen.x &&
                        mousePos.y >= minScreen.y && mousePos.y <= maxScreen.y;

		// handle dragging if in bounds
		if (inBounds)
		{
			targetIntensity = main.hoverIntensity;
			targetScale = main.hoverScale;
		}
		else
		{
			targetIntensity = main.defaultIntensity;	
			targetScale = 1;
		}
		smoothedIntensity = Mathf.Lerp(smoothedIntensity, targetIntensity, main.intensitySmoothness);
		smoothedScale =		Mathf.Lerp(smoothedScale,	  targetScale,     main.scaleSmoothness);

        mat.SetColor("_EmissiveColor", color * smoothedIntensity);
		transform.localScale = smoothedScale * Vector3.one;
    }
}
