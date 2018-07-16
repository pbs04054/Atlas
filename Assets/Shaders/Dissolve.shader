Shader "Atlas/Dissolve"
{
    Properties
    {
        _MainTex("기본 텍스쳐", 2D) = "white" {}
        _NoiseTex("노이즈 텍스쳐", 2D) = "white" {}
        _Cut("Dissolve", Range(0, 1)) = 0
        _Outline("외각선 비율", Range(1, 1.5)) = 1.15
        [HDR]_OutColor("외각선 색", Color) = (1,1,1,1)
    }
    
    SubShader
    {
        Tags {"RenderType" = "Transparent" "Queue" = "Transparent" }
        
        CGPROGRAM
        #pragma surface surf Lambert alpha:fade
        
        sampler2D _MainTex;
        sampler2D _NoiseTex;
        float _Cut;
        float4 _OutColor;
        float _Outline;
        
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NoiseTex;
        };
        
        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            float4 noise = tex2D(_NoiseTex, IN.uv_NoiseTex);
            o.Albedo = c.rgb;
            
            float alpha;
            if(noise. r >= _Cut)
                alpha = 1;
            else
                alpha = 0;
                
            float outline;
            if(noise.r >= _Cut * _Outline)
                outline = 0;
            else
                outline = 1;
                
            o.Emission = outline * _OutColor.rbg;
            o.Alpha = alpha;
        }
        
        ENDCG
        
    }
    FallBack "Diffuse"
}