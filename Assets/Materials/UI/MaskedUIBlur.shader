Shader "HDRP/Unlit/FrostedGlass"
{
    Properties
    {
        _Radius ("Radius", Range(1, 64)) = 8
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "ForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

            TEXTURE2D_X(_CameraColorTexture);
            SAMPLER(sampler_CameraColorTexture);

            float _Radius;

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                return o;
            }

            float4 SampleCamera(float2 uv)
            {
                uint2 pixel = uint2(uv * _ScreenSize.xy);
                return LOAD_TEXTURE2D_X(_CameraColorTexture, pixel);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.positionCS.xy / input.positionCS.w;
                uv = uv * 0.5 + 0.5;

                float4 sum = SampleCamera(uv);
                int count = 1;

                float2 texel = 1.0 / _ScreenSize.xy;

                for (int i = 1; i <= _Radius; i++)
                {
                    float2 o = texel * i;
                    sum += SampleCamera(uv + o);
                    sum += SampleCamera(uv - o);
                    sum += SampleCamera(uv + float2(o.x, -o.y));
                    sum += SampleCamera(uv + float2(-o.x, o.y));
                    count += 4;
                }

                float4 color = sum / count;
                color.a = 1.0;
                return color;
            }
            ENDHLSL
        }
    }
}
