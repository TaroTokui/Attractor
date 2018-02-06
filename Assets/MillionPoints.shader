Shader "Custom/MillionPoints"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_Smoothness("Smoothness", Range(0, 1)) = 0
		_Metallic("Metallic", Range(0, 1)) = 0
	}
	SubShader
	{
		//Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }

		//Cull Off

		CGPROGRAM

		//#pragma surface surf Standard vertex:vert addshadow alpha:fade
		#pragma surface surf Standard vertex:vert addshadow nolightmap alpha:fade
		#pragma instancing_options procedural:setup
		#pragma target 3.5

		#if SHADER_TARGET >= 35 && (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_PSSL) || defined(SHADER_API_SWITCH) || defined(SHADER_API_VULKAN) || (defined(SHADER_API_METAL) && defined(UNITY_COMPILER_HLSLCC)))
		#define SUPPORT_STRUCTUREDBUFFER
		#endif

		#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) && defined(SUPPORT_STRUCTUREDBUFFER)
		#define ENABLE_INSTANCING
		#endif

		struct appdata
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 tangent : TANGENT;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;
			uint vid : SV_VertexID;
			fixed4 color : COLOR;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct ParticleData
		{
			float3 Position;
			float3 Velocity;
			float3 Albedo;
			float Life;
			bool isActive;
		};

		struct Input
		{
			float vface : VFACE;
			fixed4 color : COLOR;
		};

		half4 _Color;
		half _Smoothness;
		half _Metallic;

		float3 _MeshScale;

		#if defined(ENABLE_INSTANCING)
		StructuredBuffer<ParticleData> _ParticleDataBuffer;
		#endif

		void vert(inout appdata v)
		{
			#if defined(ENABLE_INSTANCING)
			// スケールと位置(平行移動)を適用
			ParticleData p = _ParticleDataBuffer[unity_InstanceID];
			float4x4 matrix_ = (float4x4)0;
			matrix_._11_22_33_44 = float4(_MeshScale.xyz, 1.0);
			matrix_._14_24_34 += p.Position;
			v.vertex = mul(matrix_, v.vertex);

			float a = 1.0 - pow((p.Life / 5.0 - 0.5), 2);
			//a = p.isActive ? a : 0;

			v.color = fixed4(p.Albedo, p.Life / 10.0);
			#endif
		}

		void setup()
		{
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = IN.color.rgb * _Color.rgb;
			//o.Alpha = _Color.a;// IN.color.a;
			o.Alpha = IN.color.a;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Normal = float3(0, 0, IN.vface < 0 ? -1 : 1);
		}

		ENDCG
	}
	FallBack "Diffuse"
}