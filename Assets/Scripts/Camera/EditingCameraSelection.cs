using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class EditingCameraSelection : MonoBehaviour
{
	public ComputeShader outlineShader;
	public float outlineThickness;
	public Color outlineColor;
	public Camera outlinesCamera;
	RenderTexture outlinesTexture;
	RenderTexture originalImage;
	RenderTexture depthTexture;

	// Start is called before the first frame update
	void Start()
	{
		if (depthTexture == null)
		{
            depthTexture = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 24) { enableRandomWrite = true, };
            depthTexture.Create();
		}
		
		outlinesCamera.targetTexture = depthTexture;
	}
	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		//AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(depthTexture;
		outlineShader.SetTexture(0, "Depth", Shader.GetGlobalTexture("_CameraDepthTexture"));
		outlineShader.SetTexture(0, "Output", destination);
		outlineShader.Dispatch(0, originalImage.width / 8, originalImage.height / 8, 1);

	}

	// Update is called once per frame
	void Update()
	{
		
	}
}
