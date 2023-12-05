Shader "Hidden/CatDarkGame/LayerFilterRendererFeature/LayerFilterBlurRT"
{
    Properties
    { 
       _blurOffset ("BlurOffset", float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue"="Geometry" "RenderPipeline" = "UniversalPipeline" }
       
        Pass
        {
            Name  "LayerFilterBlurRT"
            Tags {"LightMode" = "LayerFilterBlurRT"}

            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            TEXTURE2D(_DownsampleTex); SAMPLER(sampler_linear_clamp);
            float4 _DownsampleTex_TexelSize;

            float _blurOffset;
            

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            }; 
            

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float4 positionOS = input.positionOS;
                float3 positionWS = TransformObjectToWorld(positionOS.xyz);
                float4 positionCS = TransformWorldToHClip(positionWS);

                output.positionCS = positionCS;
                output.uv = input.uv;
                return output;
            } 
             
            float4 frag(Varyings input) : SV_Target
            {
                float2 baseMapUV = input.uv.xy;

                float offset = _blurOffset;
                float4 tex_1 = SAMPLE_TEXTURE2D(_DownsampleTex, sampler_linear_clamp, baseMapUV + float2(_DownsampleTex_TexelSize.x * -offset, 0.0));
                float4 tex_2 = SAMPLE_TEXTURE2D(_DownsampleTex, sampler_linear_clamp, baseMapUV + float2(_DownsampleTex_TexelSize.x * offset, 0.0));
                float4 tex_3 = SAMPLE_TEXTURE2D(_DownsampleTex, sampler_linear_clamp, baseMapUV + float2(0.0, _DownsampleTex_TexelSize.y * -offset));
                float4 tex_4 = SAMPLE_TEXTURE2D(_DownsampleTex, sampler_linear_clamp, baseMapUV + float2(0.0, _DownsampleTex_TexelSize.y * offset));
                
                float4 finalColor = (tex_1 + tex_2 + tex_3 + tex_4) * 0.25f;
                return finalColor;
            }
            
            ENDHLSL
        }
    }
}

  