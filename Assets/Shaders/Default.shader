﻿Shader "Atlas/Full"
{
    Properties
    {
        _MainTex("기본 텍스쳐", 2D) = "white" {}
        _NoiseTex("노이즈 텍스쳐", 2D) = "white" {}
        _NormalTex("노말 텍스쳐", 2D) = "bump" {}
        _Cut("디졸브 비율", Range(0.001, 1)) = 0.001
        _Outline("외각선 비율", Range(1, 1.5)) = 1.15
        [HDR]_OutColor("외각선 색", Color) = (1,1,1,1)
        [HDR]_RimColor ("림라이트 색", Color) = (1,1,1,1)
		_RimPower("림라이트 Power", Range(1,100)) = 3
    }
    
    SubShader
    {
        Tags {"RenderType" = "Transparent" "Queue" = "Transparent" }
                
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
    
        CGPROGRAM
        #pragma surface surf Lambert alpha:fade alphatest:_Cutoff
        
        sampler2D _MainTex;
        sampler2D _NoiseTex;
        sampler2D _NormalTex;
        float _Cut;
        float4 _OutColor;
        float _Outline;
        float4 _RimColor;
        float _RimPower;
        
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NoiseTex;
            float2 uv_NormalTex;
            float3 viewDir;
        };
        
        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 normal = tex2D(_NormalTex, IN.uv_NormalTex); 
            o.Normal = UnpackNormal(normal);
            float4 noise = tex2D(_NoiseTex, IN.uv_NoiseTex);
            float rim = saturate(dot(o.Normal, IN.viewDir));
            o.Albedo = c.rgb;
            
            float alpha;
            if(noise.r >= _Cut)
                alpha = 1;
            else
                alpha = 0;
                
            float outline;
            if(noise.r >= _Cut * _Outline)
                outline = 0;
            else
                outline = 1;
                
            o.Emission = outline * _OutColor.rbg + pow(1-rim, _RimPower) * _RimColor.rgb;
            o.Alpha = alpha;
        }
        
        ENDCG
        
        
        
    }
    FallBack "Diffuse"
}