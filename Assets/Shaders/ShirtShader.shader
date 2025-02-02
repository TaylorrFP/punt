
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
	Depth();
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
	
	SamplerState g_sSampler0 < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >;
	CreateInputTexture2D( ShirtMask, Srgb, 8, "None", "_mask", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Text_PlayerName, Srgb, 8, "None", "_mask", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Text_PlayerNumberBack, Srgb, 8, "None", "_mask", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Text_FrontSponsor, Srgb, 8, "None", "_mask", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Text_Badge, Srgb, 8, "None", "_mask", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Text_ShotsNumber, Srgb, 8, "None", "_mask", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Diffse, Srgb, 8, "None", "_mask", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Normal, Linear, 8, "NormalizeNormals", "_normal", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Roughness, Srgb, 8, "None", "_mask", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Metalness, Srgb, 8, "None", "_mask", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( AmbientOcclusion, Srgb, 8, "None", "_mask", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	Texture2D g_tShirtMask < Channel( RGBA, Box( ShirtMask ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tText_PlayerName < Channel( RGBA, Box( Text_PlayerName ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tText_PlayerNumberBack < Channel( RGBA, Box( Text_PlayerNumberBack ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tText_FrontSponsor < Channel( RGBA, Box( Text_FrontSponsor ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tText_Badge < Channel( RGBA, Box( Text_Badge ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tText_ShotsNumber < Channel( RGBA, Box( Text_ShotsNumber ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tDiffse < Channel( RGBA, Box( Diffse ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tNormal < Channel( RGBA, Box( Normal ), Linear ); OutputFormat( DXT5 ); SrgbRead( False ); >;
	Texture2D g_tRoughness < Channel( RGBA, Box( Roughness ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tMetalness < Channel( RGBA, Box( Metalness ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tAmbientOcclusion < Channel( RGBA, Box( AmbientOcclusion ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	TextureAttribute( LightSim_DiffuseAlbedoTexture, g_tDiffse )
	TextureAttribute( RepresentativeTexture, g_tDiffse )
	float4 g_vColor_Primary < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 0.00, 0.00, 1.00 ); >;
	float4 g_vColor_Secondary < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 0.00, 1.00, 0.17, 1.00 ); >;
	float4 g_vColor_Tertiary < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 0.00, 0.02, 1.00, 1.00 ); >;
	float g_flRoughnessScale < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
	
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
		
		float4 l_0 = g_vColor_Primary;
		float4 l_1 = g_vColor_Secondary;
		float4 l_2 = Tex2DS( g_tShirtMask, g_sSampler0, i.vTextureCoords.xy );
		float l_3 = l_2.x;
		float4 l_4 = lerp( l_0, l_1, l_3 );
		float4 l_5 = g_vColor_Tertiary;
		float l_6 = l_2.y;
		float4 l_7 = lerp( l_4, l_5, l_6 );
		float2 l_8 = i.vTextureCoords.zw * float2( 1, 1 );
		float4 l_9 = Tex2DS( g_tText_PlayerName, g_sSampler0, l_8 );
		float4 l_10 = lerp( l_7, l_9, l_9.a );
		float2 l_11 = i.vTextureCoords.zw * float2( 1, 1 );
		float4 l_12 = Tex2DS( g_tText_PlayerNumberBack, g_sSampler0, l_11 );
		float4 l_13 = lerp( l_10, l_12, l_12.a );
		float2 l_14 = i.vTextureCoords.zw * float2( 1, 1 );
		float4 l_15 = Tex2DS( g_tText_FrontSponsor, g_sSampler0, l_14 );
		float4 l_16 = lerp( l_13, l_15, l_15.a );
		float2 l_17 = i.vTextureCoords.zw * float2( 1, 1 );
		float4 l_18 = Tex2DS( g_tText_Badge, g_sSampler0, l_17 );
		float4 l_19 = lerp( l_16, l_18, l_18.a );
		float2 l_20 = i.vTextureCoords.zw * float2( 1, 1 );
		float4 l_21 = Tex2DS( g_tText_ShotsNumber, g_sSampler0, l_20 );
		float4 l_22 = lerp( l_19, l_21, l_21.a );
		float4 l_23 = Tex2DS( g_tDiffse, g_sSampler0, i.vTextureCoords.xy );
		float4 l_24 = l_22 * l_23;
		float4 l_25 = Tex2DS( g_tNormal, g_sSampler0, i.vTextureCoords.xy );
		float3 l_26 = DecodeNormal( l_25.xyz );
		float4 l_27 = Tex2DS( g_tRoughness, g_sSampler0, i.vTextureCoords.xy );
		float l_28 = g_flRoughnessScale;
		float l_29 = 1 - l_28;
		float4 l_30 = l_27 * float4( l_29, l_29, l_29, l_29 );
		float4 l_31 = Tex2DS( g_tMetalness, g_sSampler0, i.vTextureCoords.xy );
		float4 l_32 = Tex2DS( g_tAmbientOcclusion, g_sSampler0, i.vTextureCoords.xy );
		
		m.Albedo = l_24.xyz;
		m.Opacity = 1;
		m.Normal = l_26;
		m.Roughness = l_30.x;
		m.Metalness = l_31.x;
		m.AmbientOcclusion = l_32.x;
		
		
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
