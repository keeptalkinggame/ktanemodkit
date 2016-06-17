// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_Lightmap', a built-in variable
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

// Unlit textured shader, with lightmap support
//
// Should be functionally identical to Mobile/Unlit (Supports Lightmap) with
// the difference that this is NOT a Fixed Function shader, which PS4 does not support
//
// - no lighting
// - lightmap support
// - textured

Shader "KT/Unlit/TexturedLightmap" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}

SubShader {
	Tags { "RenderType"="Opaque" }
	LOD 100
	
	Lighting Off
	
	Pass {  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 textureUV : TEXCOORD0;
				half2 lightmapUV : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			// sampler2D unity_Lightmap;
			// float4 unity_LightmapST;
			
			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.textureUV = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.lightmapUV = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 color = tex2D(_MainTex, i.textureUV.xy);
				fixed4 lightmap = UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lightmapUV.xy);

				color.rgb = color.rgb * DecodeLightmap(lightmap);

				return color;
			}
		ENDCG
	}
}

}
