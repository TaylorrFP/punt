
HEADER
{
	Description = "";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	VrForward();
	Depth(); 
	ToolsVis( S_MODE_TOOLS_VIS );
	ToolsWireframe( "vr_tools_wireframe.shader" );
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 0
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 0
	#endif
	
	#include "common/shared.hlsl"
	#include "procedural.hlsl"

	#define S_UV2 1
	#define CUSTOM_MATERIAL_INPUTS
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
	float4 vColor : COLOR0 < Semantic( Color ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput v )
	{
		PixelInput i = ProcessVertex( v );
		i.vPositionOs = v.vPositionOs.xyz;
		i.vColor = v.vColor;

		ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v );
		i.vTintColor = extraShaderData.vTint;

		VS_DecodeObjectSpaceNormalAndTangent( v, i.vNormalOs, i.vTangentUOs_flTangentVSign );

		return FinalizeVertex( i );
	}
}

PS
{
	#include "common/pixel.hlsl"
	
	float4 g_vRimColour < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 1.00, 1.00, 1.00 ); >;
	float g_flRimPower < UiGroup( ",0/,0/0" ); Default1( 0.26939595 ); Range1( 0, 10 ); >;
	float g_flRimAmount < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
	float g_flSaturation < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 3 ); >;
	float g_flLightness < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 3 ); >;
	float g_flRoughness < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
	float g_flMetalness < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
		
	float3 RGB2HSV( float3 c )
	{
	    float4 K = float4( 0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0 );
	    float4 p = lerp( float4( c.bg, K.wz ), float4( c.gb, K.xy ), step( c.b, c.g ) );
	    float4 q = lerp( float4( p.xyw, c.r ), float4( c.r, p.yzx ), step( p.x, c.r ) );
	
	    float d = q.x - min( q.w, q.y );
	    float e = 1.0e-10;
	    return float3( abs( q.z + ( q.w - q.y ) / ( 6.0 * d + e ) ), d / ( q.x + e ), q.x );
	}
	
	float3 HSV2RGB( float3 c )
	{
	    float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
	    float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
	    return c.z * lerp( K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y );
	}
	
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		Material m = Material::Init();
		m.Albedo = float3( 1, 1, 1 );
		m.Normal = float3( 0, 0, 1 );
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = float3( 0, 0, 0 );
		m.Transmission = 0;
		
		float4 l_0 = g_vRimColour;
		float l_1 = g_flRimPower;
		float3 l_2 = CalculatePositionToCameraDirWs( i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz );
		float3 l_3 = pow( 1.0 - dot( normalize( i.vNormalWs ), normalize( l_2 ) ), l_1 );
		float4 l_4 = l_0 * float4( l_3, 0 );
		float l_5 = g_flRimAmount;
		float4 l_6 = l_4 * float4( l_5, l_5, l_5, l_5 );
		float3 l_7 = i.vColor.rgb;
		float l_8 = 1 / 0.45454547;
		float3 l_9 = pow( l_7, float3( l_8, l_8, l_8 ) );
		float3 l_10 = RGB2HSV( l_9 );
		float l_11 = l_10.x;
		float l_12 = l_10.y;
		float l_13 = g_flSaturation;
		float l_14 = l_12 * l_13;
		float l_15 = l_10.z;
		float l_16 = g_flLightness;
		float l_17 = l_15 * l_16;
		float3 l_18 = float3( l_11, l_14, l_17 );
		float3 l_19 = HSV2RGB( l_18 );
		float4 l_20 = l_6 + float4( l_19, 0 );
		float l_21 = g_flRoughness;
		float l_22 = g_flMetalness;
		
		m.Albedo = l_20.xyz;
		m.Opacity = 1;
		m.Roughness = l_21;
		m.Metalness = l_22;
		m.AmbientOcclusion = 1;
		
		m.AmbientOcclusion = saturate( m.AmbientOcclusion );
		m.Roughness = saturate( m.Roughness );
		m.Metalness = saturate( m.Metalness );
		m.Opacity = saturate( m.Opacity );

		// Result node takes normal as tangent space, convert it to world space now
		m.Normal = TransformNormal( m.Normal, i.vNormalWs, i.vTangentUWs, i.vTangentVWs );

		// for some toolvis shit
		m.WorldTangentU = i.vTangentUWs;
		m.WorldTangentV = i.vTangentVWs;
        m.TextureCoords = i.vTextureCoords.xy;
		
		return ShadingModelStandard::Shade( i, m );
	}
}
