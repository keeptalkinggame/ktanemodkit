Shader "KT/Outline" { // Based on http://wiki.unity3d.com/index.php?title=Outlined_Diffuse_3
	Properties {
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_Outline ("Outline width", Range (.000, 0.015)) = .000
	}
 
CGINCLUDE
#include "UnityCG.cginc"
 
struct appdata {
	float4 vertex : POSITION;
	float3 normal : NORMAL;
};
 
struct v2f {
	float4 pos : SV_POSITION;
	float4 color : COLOR;
};
 
uniform float _Outline;
uniform float4 _OutlineColor;
 
v2f vert(appdata v) {
	// just make a copy of incoming vertex data but scaled according to normal direction
	v2f o;

	if(_Outline != 0)
	{
		float4 homoPos = mul(UNITY_MATRIX_MV, v.vertex);
		// converting to non-homo space might be possible by just removing w... But I'm not sure it's guarenteed that w is 1, so I'm playing it safe
		float3 nonHomoPos = homoPos.xyz / homoPos.w;
		
		// no need to renormalize, as UNITY_MATRIX_IT_MV is always orthogonal
		// http://forum.unity3d.com/threads/vec3-normalw-normalize-gl_normalmatrix-gl_normal-in-surface-shader.136586/
		// http://www.lighthouse3d.com/tutorials/glsl-tutorial/normalization-issues/
		float3 viewSpaceNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
		
		// Performing translation along normal in view space rather than local space so that scale won't affect the outline's shape.
		nonHomoPos += viewSpaceNormal * _Outline;
		
		o.pos = mul(UNITY_MATRIX_P, float4(nonHomoPos, 1));
	}
	else
	{
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	}
	
	o.color = _OutlineColor;
	
	return o;
}
ENDCG
 
	SubShader {
		Tags {"Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent"}
 
 
		// note that a vertex shader is specified here but its using the one above
		Pass {
			Name "OUTLINE"
			Tags { "LightMode" = "Always" }
			Cull Front
			ZWrite On
			ColorMask RGB
			Blend SrcAlpha OneMinusSrcAlpha
			//Offset 50,50
 
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			half4 frag(v2f i) : SV_Target { return i.color; }
			ENDCG
		}
	}
 
	Fallback "Diffuse"
}