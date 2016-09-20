// Combination of Mobile/Diffuse and LightingLambert, but added in support
// for blending with a second unlit texture.

// Mobile/Diffuse:
// Simplified Diffuse shader. Differences from regular Diffuse one:
// - no Main Color

Shader "KT/Blend Lit and Unlit Vertex Color" {
Properties {
	_MainTex ("Lit Base (RGB)", 2D) = "white" {}
    _UnlitTex ("Unlit Base (RGB)", 2D) = "white" {}
}
SubShader {
	Tags { "RenderType"="Opaque" }
	LOD 150

CGPROGRAM
// Mobile improvement: noforwardadd
// http://answers.unity3d.com/questions/1200437/how-to-make-a-conditional-pragma-surface-noforward.html
// http://gamedev.stackexchange.com/questions/123669/unity-surface-shader-conditinally-noforwardadd
#pragma surface surf BlendLitUnlit

sampler2D _MainTex;
sampler2D _UnlitTex;

struct Input {
	float2 uv_MainTex;
	float4 color : COLOR;
};

struct SurfaceOutputCustom {
    half3 Albedo;
    half3 Normal;
    half3 Emission;
    half Specular;
    half Gloss;
    half Alpha;
    // Custom fields:
    half3 Unlit;
};

void surf (Input IN, inout SurfaceOutputCustom o) {
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
	o.Albedo = c.rgb;
	o.Alpha = IN.color.a;
	o.Unlit = tex2D(_UnlitTex, IN.uv_MainTex);
}

half4 LightingBlendLitUnlit (SurfaceOutputCustom s, half3 lightDir, half atten)
{
	fixed diff = max (0, dot (s.Normal, lightDir));
	
	fixed3 unlit = s.Unlit.rgb * s.Alpha;
	
	fixed4 c;
	c.rgb = s.Albedo * _LightColor0.rgb * (diff * atten) * (1 - s.Alpha) + unlit;
	c.a = 1;
	
	return c;
}

ENDCG
}

Fallback "Mobile/Diffuse"
}
