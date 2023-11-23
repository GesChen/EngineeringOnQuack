using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlineEffect : MonoBehaviour
{
    public Shader Post_Outline;
    public Shader DrawSimple;
    Camera AttachedCamera;
    Camera OutlineCamera;
    void Start()
    {
        AttachedCamera = GetComponent<Camera>();
        OutlineCamera = new GameObject("Outline Camera").AddComponent<Camera>();
        //OutlineCamera.enabled = false;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Debug.Log("rendering");
        //set up a temporary camera
        OutlineCamera.CopyFrom(AttachedCamera);
        OutlineCamera.clearFlags = CameraClearFlags.Color;
        OutlineCamera.backgroundColor = Color.black;

        //cull any layer that isn't the outline
        OutlineCamera.cullingMask = 1 << LayerMask.NameToLayer("Outline");

        //make the temporary rendertexture
        RenderTexture TempRT = new(source.width, source.height, 0, RenderTextureFormat.R8);

        //put it to video memory
        TempRT.Create();

        //set the camera's target texture when rendering
        OutlineCamera.targetTexture = TempRT;

        Debug.Log(OutlineCamera.targetTexture);

        //render all objects this camera can render, but with our custom shader.
        OutlineCamera.RenderWithShader(DrawSimple, "");

        //copy the temporary RT to the final image
        Graphics.Blit(TempRT, destination);

        //release the temporary RT
        TempRT.Release();
    }
}
