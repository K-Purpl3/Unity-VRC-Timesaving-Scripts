//do not claim as your own, all credits go to https://github.com/K-Purpl3
Shader "Custom/Pixel3DVoxelShader"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}

        _Glossiness ("Smoothness", Range(0,1)) = 0.05
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _PixelSize ("Texture Pixel Size", Range(64,1024)) = 256

        _VoxelNear ("Voxel Density Near", Range(8,128)) = 64
        _VoxelFar  ("Voxel Density Far",  Range(4,64))  = 16
        _VoxelFadeDistance ("Voxel Fade Distance", Range(1,10)) = 4

        _UseGrayscale ("Use Grayscale", Range(0,1)) = 0
        _UseNoise ("Use Noise", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float _PixelSize;

        float _VoxelNear;
        float _VoxelFar;
        float _VoxelFadeDistance;

        float _UseGrayscale;
        float _UseNoise;

        // ----------------------------------------------------
        // Utility functions
        // ----------------------------------------------------

        float hash21(float2 p)
        {
            return frac(sin(dot(p, float2(12.9898,78.233))) * 43758.5453);
        }

        float3 SnapNormal(float3 n)
        {
            n = normalize(n);
            float3 an = abs(n);

            if (an.x > an.y && an.x > an.z)
                return float3(sign(n.x), 0, 0);
            else if (an.y > an.x && an.y > an.z)
                return float3(0, sign(n.y), 0);
            else
                return float3(0, 0, sign(n.z));
        }

        // ----------------------------------------------------
        // Vertex function (THIS is the 3D pixelation core)
        // ----------------------------------------------------

        void vert (inout appdata_full v)
        {
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float dist = distance(_WorldSpaceCameraPos, worldPos);

            float t = saturate(dist / _VoxelFadeDistance);
            float voxelSize = lerp(_VoxelNear, _VoxelFar, t);

            v.vertex.xyz = floor(v.vertex.xyz * voxelSize) / voxelSize;
            //v.normal = SnapNormal(v.normal);
        }

        // ----------------------------------------------------
        // Surface function (color / texture / pixel logic)
        // ----------------------------------------------------

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;
            uv = floor(uv * _PixelSize) / _PixelSize;

            fixed4 c = tex2D(_MainTex, uv) * _Color;

            // Color or grayscale
            float3 baseColor = c.rgb;
            float gray = dot(baseColor, float3(0.299, 0.587, 0.114));
            baseColor = lerp(baseColor, gray.xxx, _UseGrayscale);

            // Optional noise / glitch
            float noise = hash21(uv * 100.0);
            float threshold = step(0.8, noise);
            baseColor = lerp(baseColor, float3(1,1,1), threshold * 0.4 * _UseNoise);

            // Optional color quantization (retro feel)
            float steps = 8.0;
            baseColor = floor(baseColor * steps) / steps;

            o.Albedo = baseColor;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

