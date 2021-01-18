// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Grass" {
	Properties {		
		[Header(Shading)]
		_TopColor("Top Color", Color) = (1,1,1,1)
		_BottomColor("Bottom Color", Color) = (1,1,1,1)
		_TranslucentGain("Translucent Gain", Range(0,1)) = 0.5
		[Space]
		_GrassMask("Grass Mask", 2D) = "white" {}
		_GrassMaskThreshold("Mask Threshold", Range(0,1)) = 0.5
		_MaskHeightImpact("Grass Mask Height Impact", Range(0,1)) = 0.5
		[Space]
		_TessellationUniform("Tessellation Uniform", Range(1, 64)) = 1
		_SlopeLimit("Slope Limit", Float) = 45
		_minDist("Min Distance", Float) = 0
		_maxDist("Max Distance", Float) = 100
		_BeachLimit("Minimum Height", Float) = 5
		_HeightLimit("Maximum Height", Float) = 30
		_DistanceFeather("Distance Feather", Range(0, 10)) = 5.5
		[Header(Blades)]
		_BladeWidth("Blade Width", Float) = 0.05
		_BladeWidthRandom("Blade Width Random", Float) = 0.02
		_BladeHeight("Blade Height", Float) = 0.5
		_BladeHeightRandom("Blade Height Random", Float) = 0.3
		_BladeForward("Blade Forward Amount", Float) = 0.38
		_BladeCurve("Blade Curvature Amount", Range(1, 4)) = 2
		_BendRotationRandom("Bend Rotation Random", Range(0, 1)) = 0.2
		[Header(Wind)]
		_WindDistortionMap("Wind Distortion Map", 2D) = "white" {}
		_WindStrength("Wind Strength", Float) = 1
		_WindFrequency("Wind Frequency", Vector) = (0.05, 0.05, 0, 0)
	}	

	CGINCLUDE
	#include "UnityCG.cginc"
	#include "Autolight.cginc"
	#include "CustomTessellation.cginc"

	struct geometryOutput {
		float4 pos : SV_POSITION;
		float4 color : TEXCOORD2;
		#if UNITY_PASS_FORWARDBASE		
		float3 normal : NORMAL;
		float2 uv : TEXCOORD0;
		unityShadowCoord4 _ShadowCoord : TEXCOORD1;
		#endif
		UNITY_FOG_COORDS(3)
	};

	float rand(float3 co) {
		return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
	}

	float3x3 AngleAxis3x3(float angle, float3 axis) {
		float c, s;
		sincos(angle, s, c);

		float t = 1 - c;
		float x = axis.x;
		float y = axis.y;
		float z = axis.z;

		return float3x3(
			t * x * x + c, t * x * y - s * z, t * x * z + s * y,
			t * x * y + s * z, t * y * y + c, t * y * z - s * x,
			t * x * z - s * y, t * y * z + s * x, t * z * z + c
			);
	}

	geometryOutput VertexOutput(float3 pos, float4 color, float3 normal, float2 uv) {
		geometryOutput o;

		o.pos = UnityObjectToClipPos(pos);
		o.color = color;
		#if UNITY_PASS_FORWARDBASE
		o.normal = UnityObjectToWorldNormal(normal);
		o.uv = uv;
		o._ShadowCoord = ComputeScreenPos(o.pos);
		#elif UNITY_PASS_SHADOWCASTER
		o.pos = UnityApplyLinearShadowBias(o.pos);
		#endif
		UNITY_TRANSFER_FOG(o, o.pos);
		return o;
	}

	geometryOutput GenerateGrassVertex(float3 vertexPosition, float width, float height, float forward, float2 uv, float3x3 transformMatrix) {
		float3 tangentPoint = float3(width, forward, height);

		float4 color = float4(0, 0, 0, 0);

		float3 tangentNormal = normalize(float3(0, -1, forward));

		float3 localPosition = vertexPosition + mul(transformMatrix, tangentPoint);
		float3 localNormal = mul(transformMatrix, tangentNormal);
		return VertexOutput(localPosition, color, localNormal, uv);
	}

	float _SlopeLimit;
	float _BeachLimit;
	float _HeightLimit;

	float _BladeHeight;
	float _BladeHeightRandom;
	float _BladeWidthRandom;
	float _BladeWidth;

	float _BladeForward;
	float _BladeCurve;
	float _BendRotationRandom;

	sampler2D _WindDistortionMap;
	float4 _WindDistortionMap_ST;
	float _WindStrength;
	float2 _WindFrequency;

	sampler2D _GrassMask;
	float _GrassMaskThreshold;
	float _MaskHeightImpact;

	#define BLADE_SEGMENTS 2

	float _DistanceFeather;

	// Geometry program that takes in a single triangle and outputs a blade
	// of grass at that triangle first vertex position, aligned to the vertex's normal.
	[maxvertexcount(BLADE_SEGMENTS * 2 + 1)]
	void geo(triangle vertexOutput IN[3], inout TriangleStream<geometryOutput> triStream) {
		float3 pos = IN[0].vertex.xyz;

		float3 worldPos =  mul(unity_ObjectToWorld, float4(IN[0].vertex.xyz, 1.0)).xyz;

		float3 worldNormal = mul(unity_ObjectToWorld, float4(IN[0].normal, 0.0)).xyz;
		float dist = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld, IN[0].vertex).xyz);

		float grassMask = tex2Dlod(_GrassMask, float4(pos.xz / 31, 0, 0));

		//Feather's the lower boundary to make transition smoother based on the grass mask
		float grassMaskAdditive = 1 - saturate((worldPos.y - _BeachLimit) / (_BeachLimit + _DistanceFeather - _BeachLimit));

		int shouldCreateGrass = sign(worldNormal.y - _SlopeLimit) + sign(_maxDist - dist) + sign(worldPos.y - _BeachLimit) + sign(_HeightLimit - worldPos.y) + sign(grassMask - _GrassMaskThreshold - grassMaskAdditive);
		//int shouldCreateGrass = sign(worldNormal.y - _SlopeLimit) + sign(_maxDist - dist) + sign(worldPos.y - _BeachLimit) + sign(_HeightLimit - worldPos.y);

		if (shouldCreateGrass == 5 && (IN[0].color.r * 255) == 1) {
			// Each blade of grass is constructed in tangent space with respect
			// to the emitting vertex's normal and tangent vectors, where the width
			// lies along the X axis and the height along Z.

			// Construct random rotations to point the blade in a direction.
			float3x3 facingRotationMatrix = AngleAxis3x3(rand(pos) * UNITY_TWO_PI, float3(0, 0, 1));
			// Matrix to bend the blade in the direction it's facing.
			float3x3 bendRotationMatrix = AngleAxis3x3(rand(pos.zzx) * _BendRotationRandom * UNITY_PI * 0.5, float3(-1, 0, 0));

			// Sample the wind distortion map, and construct a normalized vector of its direction.
			float2 uv = pos.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindFrequency * _Time.y;
			float2 windSample = (tex2Dlod(_WindDistortionMap, float4(uv, 0, 0)).xy * 2 - 1) * _WindStrength;
			float3 wind = normalize(float3(windSample.x, windSample.y, 0));

			float3x3 windRotation = AngleAxis3x3(UNITY_PI * windSample, wind);

			// Construct a matrix to transform our blade from tangent space
			// to local space; this is the same process used when sampling normal maps.
			float3 vNormal = IN[0].normal;
			float4 vTangent = IN[0].tangent;
			float3 vBinormal = cross(vNormal, vTangent) * vTangent.w;

			float3x3 tangentToLocal = float3x3(
				vTangent.x, vBinormal.x, vNormal.x,
				vTangent.y, vBinormal.y, vNormal.y,
				vTangent.z, vBinormal.z, vNormal.z
				);

			// Construct full tangent to local matrix, including our rotations.
			// Construct a second matrix with only the facing rotation; this will be used 
			// for the root of the blade, to ensure it always faces the correct direction.
			float3x3 transformationMatrix = mul(mul(mul(tangentToLocal, windRotation), facingRotationMatrix), bendRotationMatrix);
			float3x3 transformationMatrixFacing = mul(tangentToLocal, facingRotationMatrix);

			float trueGrassMask = lerp(1 - _MaskHeightImpact, 1, grassMask);
				
			float height = ((rand(pos.zyx) * 2 - 1) * _BladeHeightRandom + _BladeHeight) * trueGrassMask;
			float width = ((rand(pos.xzy) * 2 - 1) * _BladeWidthRandom + _BladeWidth) * trueGrassMask;
			float forward = rand(pos.yyz) * _BladeForward;

			for (int i = 0; i < BLADE_SEGMENTS; i++) {
				float t = i / (float)BLADE_SEGMENTS;

				float segmentHeight = height * t;
				float segmentWidth = width * (1 - t);
				float segmentForward = pow(t, _BladeCurve) * forward;

				// Select the facing-only transformation matrix for the root of the blade.
				float3x3 transformMatrix = i == 0 ? transformationMatrixFacing : transformationMatrix;

				triStream.Append(GenerateGrassVertex(pos, segmentWidth, segmentHeight, segmentForward, float2(0, t), transformMatrix));
				triStream.Append(GenerateGrassVertex(pos, -segmentWidth, segmentHeight, segmentForward, float2(1, t), transformMatrix));
			}

			// Add the final vertex as the tip.
			triStream.Append(GenerateGrassVertex(pos, 0, height, forward, float2(0.5, 1), transformationMatrix));
		}
	}
	ENDCG

		SubShader
	{
		Cull Off

		Pass
		{
			Tags
			{
				"RenderType" = "Opaque"
				"LightMode" = "ForwardBase"
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geo
			#pragma fragment frag
			#pragma hull hull
			#pragma domain domain
			#pragma target 4.6
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#include "Lighting.cginc"

			float4 _TopColor;
			float4 _BottomColor;
			float _TranslucentGain;

			float4 frag(geometryOutput i,  fixed facing : VFACE) : SV_Target
			{
				float3 normal = facing > 0 ? i.normal : -i.normal;

				float shadow = SHADOW_ATTENUATION(i);
				float NdotL = saturate(saturate(dot(normal, _WorldSpaceLightPos0)) + _TranslucentGain) * shadow;

				float3 ambient = ShadeSH9(float4(normal, 1));
				float4 lightIntensity = NdotL * _LightColor0 + float4(ambient, 1);
				float4 col = lerp(_BottomColor * lightIntensity, _TopColor * lightIntensity, i.uv.y);
					
				UNITY_APPLY_FOG(i.fogCoord, col);
				UNITY_OPAQUE_ALPHA(col.a);

				return col;
			}
			ENDCG
		}

		Pass
		{
			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geo
			#pragma fragment frag
			#pragma hull hull
			#pragma domain domain
			#pragma target 4.6
			#pragma multi_compile_shadowcaster

			float4 frag(geometryOutput i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}

			ENDCG
		}
	}
	
}