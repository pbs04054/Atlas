Shader "Atlas/RimLight" {
	Properties {
        _MainTex("기본 텍스쳐", 2D) = "white" {}
        _RimColor ("Rim Light 색", Color) = (1,1,1,1)
		_RimPower("Rim Light Power", Range(1,10)) = 3
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		
		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		float4 _RimColor;
		float _RimPower;

		struct Input {
			float2 uv_MainTex;
			float3 viewDir;
		};
		
		void surf (Input IN, inout SurfaceOutput o)
		{
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			float rim = saturate(dot(o.Normal, IN.viewDir));
			o.Emission = pow(1-rim, _RimPower) * _RimColor.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
