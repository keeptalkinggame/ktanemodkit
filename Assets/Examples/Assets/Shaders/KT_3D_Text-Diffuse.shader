Shader "GUI/KT 3D Text Diffuse" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}
SubShader {
	Tags {
		"Queue"="Transparent"
		"IgnoreProjector"="True"
		"RenderType"="Transparent"
		"PreviewType"="Plane"
	}
	LOD 150
	Cull Front
	Blend SrcAlpha OneMinusSrcAlpha

CGPROGRAM
#pragma surface surf Lambert alpha

sampler2D _MainTex;

struct Input {
	float2 uv_MainTex;
	float4 color : COLOR;
};

void surf (Input IN, inout SurfaceOutput o) {
	half4 c = tex2D(_MainTex, IN.uv_MainTex);
	o.Albedo = IN.color.rgb;
	o.Alpha = c.a * IN.color.a;
}
ENDCG
}

Fallback "Mobile/VertexLit"
}
