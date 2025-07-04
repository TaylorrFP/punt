
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
	Forward();
	Depth( S_MODE_DEPTH );
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 0
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 1
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
	
	DynamicCombo( D_RENDER_BACKFACES, 0..1, Sys( ALL ) );
	RenderState( CullMode, D_RENDER_BACKFACES ? NONE : BACK );
		
	float4 g_vColor < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 1.00, 1.00, 1.00 ); >;
	float g_flRadius < UiGroup( ",0/,0/0" ); Default1( 0.5 ); Range1( 0, 1 ); >;
	float g_flWidth < UiGroup( ",0/,0/0" ); Default1( 0.1 ); Range1( 0, 1 ); >;
	float g_flProgress < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
	
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
		
		float4 l_0 = g_vColor;
		float2 l_1 = TileAndOffsetUv( i.vTextureCoords.xy, float2( 1, 1 ), float2( -0.5, -0.5 ) );
		float l_2 = length( l_1 );
		float l_3 = g_flRadius;
		float l_4 = l_2 - l_3;
		float l_5 = abs( l_4 );
		float l_6 = g_flWidth;
		float l_7 = l_5 - l_6;
		float l_8 = fwidth( l_4 );
		float l_9 = l_7 / l_8;
		float l_10 = 1 - l_9;
		float l_11 = saturate( ( l_10 - 0 ) / ( 1 - 0 ) ) * ( 1 - 0 ) + 0;
		float l_12 = g_flProgress;
		float l_13 = 1 - l_12;
		float l_14 = l_13 * 6.283;
		float l_15 = l_1.x;
		float l_16 = l_1.y;
		float l_17 = atan2( l_15, l_16 );
		float l_18 = l_17 + 3.141;
		float l_19 = l_14 - l_18;
		float l_20 = fwidth( l_19 );
		float l_21 = l_19 / l_20;
		float l_22 = saturate( ( l_21 - 0 ) / ( 1 - 0 ) ) * ( 1 - 0 ) + 0;
		float l_23 = l_11 - l_22;
		float l_24 = saturate( ( l_23 - 0 ) / ( 1 - 0 ) ) * ( 1 - 0 ) + 0;
		
		m.Albedo = l_0.xyz;
		m.Opacity = l_24;
		m.Roughness = 1;
		m.Metalness = 0;
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
