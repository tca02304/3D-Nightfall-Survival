Shader "Custom/URP_GlowOutline"
{
    Properties
    {
        [HDR] _GlowColor ("Glow Color (HDR)", Color) = (2, 0.98, 0, 1)
        _Thickness ("Thickness (world units)", Range(0, 0.2)) = 0.03
        _PulseSpeed ("Pulse Speed", Float) = 3
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "GlowOutline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                float _Thickness;
                float _PulseSpeed;
                float _PulseAmount;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nrmWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                posWS += nrmWS * _Thickness;
                OUT.positionCS = TransformWorldToHClip(posWS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                return half4(_GlowColor.rgb * pulse, 1);
            }
            ENDHLSL
        }
    }
}
