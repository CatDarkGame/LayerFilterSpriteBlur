Shader "CatDarkGame/Sprites/LayerFilterSpriteBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlendAmount("Blur Amount(Use Sprite Alpha)", Range(0,1)) = 0.5
    }
    
    HLSLINCLUDE
    
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
       
        struct Attributes
        {
            float3 positionOS   : POSITION;
            float4 color        : COLOR;
            float2 uv           : TEXCOORD0;
        };

        struct Varyings
        {
            float4  positionCS      : SV_POSITION;
            float4  color           : COLOR;
            float2  uv              : TEXCOORD0;
            float4  screenPosition  : TEXCOORD1;
        };

        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        float4 _MainTex_ST;
        
        TEXTURE2D(_LayerFilterCopypassBufferTex); SAMPLER(sampler_LayerFilterCopypassBufferTex);

        half _BlendAmount;

        Varyings vert(Attributes attributes)
        {
            Varyings o = (Varyings)0;
               
            o.positionCS = TransformObjectToHClip(attributes.positionOS);
            o.screenPosition = ComputeScreenPos(o.positionCS);	
            o.uv = attributes.uv;
            o.color = attributes.color;
            return o;
        }
    
        half4 PrePassFragment (Varyings i) : SV_TARGET 
        {
            float2 uv = i.uv * _MainTex_ST.xy + _MainTex_ST.zw;
            half4 baseMap = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv); 
            return baseMap;
        }
        
        half4 DrawPassFragment (Varyings i) : SV_TARGET 
        {
            float2 uv = i.uv * _MainTex_ST.xy + _MainTex_ST.zw;
            half blendAmount = i.color.a * _BlendAmount;
            blendAmount = saturate(pow(blendAmount, 2.2));

            //half4 baseMap = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv); 
            half4 baseMap = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, blendAmount * 3.2); // 자연스러운 블랜딩을 위해 mipmap 샘플링 이용
            
            float2 uv_prepass = i.screenPosition.xy / i.screenPosition.w;
            half4 prepassMap = SAMPLE_TEXTURE2D(_LayerFilterCopypassBufferTex, sampler_LayerFilterCopypassBufferTex, uv_prepass); 
            
            half4 finalColor = lerp(baseMap, prepassMap, blendAmount);
            finalColor.a = baseMap.a;
            return finalColor;
        }


    
    ENDHLSL
        
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off 
        Cull Off
        

        Pass
        {
            Tags { "LightMode" = "SpriteRenderPrepass" }
            Name "PrePass"

            Blend SrcAlpha OneMinusSrcAlpha	
            HLSLPROGRAM

                #pragma vertex vert
                #pragma fragment PrePassFragment

            ENDHLSL
        }
        
        Pass
        {
            Tags { "LightMode" = "SpriteRenderDrawpass" }
            Name "DrawPass" 

            Blend SrcAlpha OneMinusSrcAlpha	
            
            HLSLPROGRAM

                #pragma vertex vert
                #pragma fragment DrawPassFragment

            ENDHLSL
        }
    }
}