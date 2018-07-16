Shader "Atlas/EmissiveBlink" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
        [HDR]_EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
        _EmissionPower("Emission Power", float) = 0
        _EmissionTimeSclae("Time Scale", float) = 1
        _EmissionSmooth("Smooth", Range(0.1,1)) = 0.3
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows

		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float2 uv_EmissionMap;
		};

		half _Glossiness;
		half _Metallic;
		half _EmissionPower;
		half _EmissionTimeSclae;
		half _EmissionSmooth;
		sampler2D _EmissionMap;
		fixed4 _EmissionColor;
		fixed4 _Color;
		

		UNITY_INSTANCING_CBUFFER_START(Props)
		UNITY_INSTANCING_CBUFFER_END

		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
			o.Emission = _EmissionColor * tex2D(_EmissionMap, IN.uv_EmissionMap) * (sin(_Time.y * 0.5 * _EmissionTimeSclae) * _EmissionSmooth + 1.1) * _EmissionPower;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
