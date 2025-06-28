
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
	
	DynamicCombo( D_RENDER_BACKFACES, 0..1, Sys( ALL ) );
	RenderState( CullMode, D_RENDER_BACKFACES ? NONE : BACK );
		
	SamplerState g_sSampler0 < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >;
	CreateInputTexture2D( Albedo, Srgb, 8, "None", "_color", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Normal, Linear, 8, "NormalizeNormals", "_normal", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Opacity, Srgb, 8, "None", "_color", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Roughness, Linear, 8, "None", "_rough", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Metalness, Srgb, 8, "None", "_metal", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( AmbientOcclusion, Srgb, 8, "None", "_ao", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	Texture2D g_tAlbedo < Channel( RGBA, Box( Albedo ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tNormal < Channel( RGBA, Box( Normal ), Linear ); OutputFormat( RGBA8888 ); SrgbRead( False ); >;
	Texture2D g_tOpacity < Channel( RGBA, Box( Opacity ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tRoughness < Channel( RGBA, Box( Roughness ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tMetalness < Channel( RGBA, Box( Metalness ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tAmbientOcclusion < Channel( RGBA, Box( AmbientOcclusion ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	TextureAttribute( LightSim_DiffuseAlbedoTexture, g_tNormal )
	TextureAttribute( RepresentativeTexture, g_tNormal )
	float g_flNormalDistort < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 1 ); >;
	float g_flFrequency < UiGroup( ",0/,0/0" ); Default1( 3.1441605 ); Range1( 0, 100 ); >;
	float2 g_vTiling < UiGroup( ",0/,0/0" ); Default2( 1,1 ); Range2( 0,0, 5,5 ); >;
	float g_flDistortion < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 10 ); >;
	float g_flPower < UiGroup( ",0/,0/0" ); Default1( 5 ); Range1( 0, 10 ); >;
	float g_flHoloStrength < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 5 ); >;
		
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
		
		float4 l_0 = Tex2DS( g_tAlbedo, g_sSampler0, i.vTextureCoords.xy );
		float4 l_1 = float4( 1, 0, 0, 1 );
		float3 l_2 = RGB2HSV( l_1 );
		float l_3 = l_2.x;
		float l_4 = g_flNormalDistort;
		float4 l_5 = Tex2DS( g_tNormal, g_sSampler0, i.vTextureCoords.xy );
		float3 l_6 = TransformNormal( DecodeNormal( l_5.xyz ), i.vNormalWs, i.vTangentUWs, i.vTangentVWs );
		float3 l_7 = float3( l_4, l_4, l_4 ) * l_6;
		float3 l_8 = CalculatePositionToCameraDirWs( i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz );
		float3 l_9 = l_7 + l_8;
		float l_10 = l_9.x;
		float l_11 = g_flFrequency;
		float l_12 = l_10 * l_11;
		float l_13 = sin( l_12 );
		float l_14 = frac( l_13 );
		float l_15 = l_3 + l_14;
		float l_16 = l_2.y;
		float l_17 = l_2.z;
		float l_18 = 0.0f;
		float4 l_19 = float4( l_15, l_16, l_17, l_18 );
		float3 l_20 = HSV2RGB( l_19 );
		float2 l_21 = g_vTiling;
		float3 l_22 = CalculatePositionToCameraDirWs( i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz );
		float3 l_23 = TransformNormal( DecodeNormal( l_5.xyz ), i.vNormalWs, i.vTangentUWs, i.vTangentVWs );
		float l_24 = g_flDistortion;
		float3 l_25 = l_23 * float3( l_24, l_24, l_24 );
		float3 l_26 = l_22 + l_25;
		float2 l_27 = TileAndOffsetUv( i.vTextureCoords.xy, l_21, l_26.xy );
		float l_28 = VoronoiNoise( l_27, 3.1415925, 10 );
		float l_29 = g_flPower;
		float l_30 = pow( l_28, l_29 );
		float3 l_31 = l_20 * float3( l_30, l_30, l_30 );
		float l_32 = g_flHoloStrength;
		float3 l_33 = l_31 * float3( l_32, l_32, l_32 );
		float4 l_34 = l_0 + float4( l_33, 0 );
		float4 l_35 = Tex2DS( g_tOpacity, g_sSampler0, i.vTextureCoords.xy );
		float3 l_36 = DecodeNormal( l_5.xyz );
		float4 l_37 = Tex2DS( g_tRoughness, g_sSampler0, i.vTextureCoords.xy );
		float4 l_38 = Tex2DS( g_tMetalness, g_sSampler0, i.vTextureCoords.xy );
		float4 l_39 = Tex2DS( g_tAmbientOcclusion, g_sSampler0, i.vTextureCoords.xy );
		
		m.Albedo = l_34.xyz;
		m.Opacity = l_35.x;
		m.Normal = l_36;
		m.Roughness = l_37.x;
		m.Metalness = l_38.x;
		m.AmbientOcclusion = l_39.x;
		
		
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
