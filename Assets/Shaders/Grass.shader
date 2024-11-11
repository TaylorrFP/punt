
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
	#define S_ALPHA_TEST 1
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
	
	SamplerState g_sSampler0 < Filter( ANISO ); AddressU( CLAMP ); AddressV( CLAMP ); >;
	SamplerState g_sSampler1 < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >;
	CreateInputTexture2D( PitchBlend, Srgb, 8, "None", "_color", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Normal, Linear, 8, "None", "_normal", ",0/,0/0", Default4( 0.50, 0.50, 1.00, 1.00 ) );
	CreateInputTexture2D( GrassNoise, Linear, 8, "None", "_color", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	Texture2D g_tPitchBlend < Channel( RGBA, Box( PitchBlend ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tNormal < Channel( RGBA, Box( Normal ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tGrassNoise < Channel( RGBA, Box( GrassNoise ), Linear ); OutputFormat( DXT5 ); SrgbRead( False ); >;
	float4 g_vGrassColourLight < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 1.00, 1.00, 1.00 ); >;
	float4 g_vGrassColourDark < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 1.00, 1.00, 1.00 ); >;
	float g_flGrassTiling < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 64 ); >;
	float g_flGrassWarpAmount < UiGroup( ",0/,0/0" ); Default1( 0.01 ); Range1( 0, 1 ); >;
	float4 g_vLineColour < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 1.00, 1.00, 1.00 ); >;
	float g_flGassAOSaturation < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 3 ); >;
	float g_flGrassAOLightness < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 1 ); >;
	float g_flMinClipFudge < UiGroup( ",0/,0/0" ); Default1( 0.01 ); Range1( 0, 1 ); >;
	float g_flRoughness < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
	float g_flMetalness < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
	float g_flGrassAOAmount < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
		
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
		
		float4 l_0 = g_vGrassColourLight;
		float4 l_1 = g_vGrassColourDark;
		float2 l_2 = i.vTextureCoords.xy * float2( 1, 1 );
		float l_3 = g_flGrassTiling;
		float2 l_4 = TileAndOffsetUv( i.vTextureCoords.xy, float2( l_3, l_3 ), float2( 0, 0 ) );
		float4 l_5 = Tex2DS( g_tNormal, g_sSampler1, l_4 );
		float2 l_6 = float2( l_5.r, l_5.g );
		float l_7 = g_flGrassWarpAmount;
		float2 l_8 = l_6 * float2( l_7, l_7 );
		float2 l_9 = l_2 + l_8;
		float l_10 = l_7 * 0.5;
		float2 l_11 = l_9 - float2( l_10, l_10 );
		float4 l_12 = Tex2DS( g_tPitchBlend, g_sSampler0, l_11 );
		float4 l_13 = lerp( l_0, l_1, l_12.r );
		float4 l_14 = g_vLineColour;
		float4 l_15 = lerp( l_13, l_14, l_12.b );
		float3 l_16 = RGB2HSV( l_15 );
		float l_17 = l_16.x;
		float l_18 = l_16.y;
		float l_19 = g_flGassAOSaturation;
		float l_20 = l_18 * l_19;
		float l_21 = l_16.z;
		float l_22 = g_flGrassAOLightness;
		float l_23 = l_21 * l_22;
		float4 l_24 = float4( l_17, l_20, l_23, 0 );
		float3 l_25 = HSV2RGB( l_24 );
		float4 l_26 = Tex2DS( g_tGrassNoise, g_sSampler1, l_4 );
		float4 l_27 = lerp( float4( l_25, 0 ), l_15, l_26.r );
		float l_28 = g_flMinClipFudge;
		float l_29 = saturate( ( l_26.r - 0 ) / ( 1 - 0 ) ) * ( 1 - l_28 ) + l_28;
		float3 l_30 = i.vColor.rgb;
		float l_31 = l_30.x;
		float l_32 = 1 / 0.45454547;
		float l_33 = pow( l_31, l_32 );
		float l_34 = l_29 - l_33;
		float l_35 = l_34 + 0.5;
		float l_36 = g_flRoughness;
		float l_37 = g_flMetalness;
		float l_38 = lerp( l_37, 0, l_12.b );
		float l_39 = g_flGrassAOAmount;
		float l_40 = lerp( 1, l_26.r, l_39 );
		
		m.Albedo = l_27.xyz;
		m.Opacity = l_35;
		m.Roughness = l_36;
		m.Metalness = l_38;
		m.AmbientOcclusion = l_40;
		
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
