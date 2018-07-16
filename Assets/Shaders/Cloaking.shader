Shader "Atlas/Cloaking"
{
	Properties
	{
		_MainTex ("텍스쳐 (R,G=X,Y 왜곡; B=마스크; A=미사용)", 2D) = "white" {}
		_Tint ("색상", Color) = (0.5,0.5,0.5,1)
		_IntensityAndScrolling ("강도 (XY); 스크롤 (ZW)", Vector) = (0.05,0.05,0.5,0.5)
		_DistanceFade ("거리별 분산 (X=가까운 정도, Y=먼 정도, ZW=미사용)", Float) = (20, 50, 0, 0)
		[Toggle(MASK)] _MASK ("텍스쳐의 B 채널을 마스크 채널로 사용?", Float) = 0
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode ("컬링", Float) = 0
	}

	SubShader
	{
		Tags {"Queue" = "Transparent" "IgnoreProjector" = "True"}
		Blend One Zero
		Lighting Off
		Fog { Mode Off }
		ZWrite Off
		LOD 200
		Cull [_CullMode]
		
		GrabPass{ "_GrabTexture" }
	
		Pass
		{  
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma shader_feature MASK
				#pragma shader_feature MIRROR_EDGE
				#pragma shader_feature DEBUGUV
				#pragma shader_feature DEBUGDISTANCEFADE

				#define ENABLE_TINT 1
				#include "UnityCG.cginc"
				#include "GrabPassDistortion.cginc"
			ENDCG
		}
	}
}
