Shader "GUI/KT 3D Text Diffuse" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}
		SubShader{
			Tags {
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
				"PreviewType" = "Plane"
				"DisableBatching" = "True"
			}
			LOD 150
			Cull Back
			Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		#pragma surface surf Lambert vertex:vert alpha

		sampler2D _MainTex;

		struct Output
		{
			float4 vertex    : POSITION;
			float3 normal    : NORMAL;
			float4 color	 : COLOR;
			float4 texcoord  : TEXCOORD0;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;
		};

		struct Input {
			float2 uv_MainTex;
			float4 color : COLOR;
		};

		void vert(inout Output o)
		{
			// Generate normals for lighting.
			o.normal = float3(0, 0, -1);
		}


		void surf(Input IN, inout SurfaceOutput o) {
			half4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = IN.color.rgb;
			o.Alpha = c.a * IN.color.a;
		}
		ENDCG
	}

		Fallback "Mobile/VertexLit"
}