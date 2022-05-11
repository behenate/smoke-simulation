Shader "Custom/coco"{
Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1) // add _Color property
    }
    SubShader {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull front 
        LOD 100
        
        
//        LOD 200
        CGPROGRAM
        // Physically based Standard lighting model
        #pragma surface surf Standard alpha
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:setup


        sampler2D _MainTex;

        struct Input {
            float4 color : COLOR;
            float2 uv_MainTex;
        };

    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        StructuredBuffer<float4> positionBuffer;
        StructuredBuffer<float4> colorBuffer;
    #endif

        void rotate2D(inout float2 v, float r)
        {
            float s, c;
            sincos(r, s, c);
            v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
        
        void setup()
        {
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            float4 data = positionBuffer[unity_InstanceID];
        
            float rotation = data.w * data.w  * 0.5f;
            rotate2D(data.xz, rotation);
        
            unity_ObjectToWorld._11_21_31_41 = float4(data.w, 0, 0, 0);
            unity_ObjectToWorld._12_22_32_42 = float4(0, data.w, 0, 0);
            unity_ObjectToWorld._13_23_33_43 = float4(0, 0, data.w, 0);
            unity_ObjectToWorld._14_24_34_44 = float4(data.xyz, 1);
            unity_WorldToObject = unity_ObjectToWorld;
            unity_WorldToObject._14_24_34 *= -1;
            unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
        #endif
        }

        half _Glossiness;
        half _Metallic;
        float4 _Color;
        void surf (Input IN, inout SurfaceOutputStandard o) {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            IN.color = colorBuffer[unity_InstanceID];
            #else
            IN.color = _Color;
            #endif
            
            o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb * IN.color;
            o.Metallic = _Metallic;
            o.Smoothness = 0;
            o.Alpha = IN.color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}