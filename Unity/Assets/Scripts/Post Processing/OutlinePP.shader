Shader "Hidden/Shader/OutlinePP"
{
    Properties
    {
        // This property is necessary to make the CommandBuffer.Blit bind the source texture to _MainTex
        _MainTex("Main Texture", 2DArray) = "grey" {}
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    // List of properties to control your post process effect
    float _Intensity;
    float _Thickness;
    float3 _Color;
    float _Power;
    float _MinDepth;
    float _MaxDepth;
    TEXTURE2D_X(_MainTex);

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        
        // Note that if HDUtils.DrawFullScreen is not used to render the post process, you don't need to call ClampAndScaleUVForBilinearPostProcessTexture.
        uint2 positionSS = input.texcoord * _ScreenSize.xy;
    
        float offset_positive =  ceil (_Thickness * .5);
        float offset_negative = -floor(_Thickness * .5);
    
        float d0 = LOAD_TEXTURE2D_X(_CameraDepthTexture, positionSS + uint2(offset_negative, offset_negative)).x;
        float d1 = LOAD_TEXTURE2D_X(_CameraDepthTexture, positionSS + uint2(offset_positive, offset_positive)).x;
        float d2 = LOAD_TEXTURE2D_X(_CameraDepthTexture, positionSS + uint2(offset_positive, offset_negative)).x;
        float d3 = LOAD_TEXTURE2D_X(_CameraDepthTexture, positionSS + uint2(offset_negative, offset_positive)).x;
    
        float d = length(float2(d1 - d0, d3 - d2));
        d = smoothstep(_MinDepth, _MaxDepth, d);
        d = 1 - pow(1 - d, _Power);

        float4 original = SAMPLE_TEXTURE2D_X(_MainTex, s_linear_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(input.texcoord.xy));
        //return float4(d, d, d, 1);
        return float4(lerp(original.xyz, _Color, d), 1);
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "OutlinePP"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment CustomPostProcess
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}
