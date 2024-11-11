//=========================================================================================================================
// Shader Header
//=========================================================================================================================
HEADER {
    Description = "Smear Effect Shader for S&box";
    Version = 1;
}

//=========================================================================================================================
// Common Variables and Constants
//=========================================================================================================================
COMMON {
float4 _Color;
float _Glossiness;
float _Metallic;

float3 _Position;
float3 _PrevPosition;
    
float _NoiseScale;
float _NoiseHeight;
}

//=========================================================================================================================
// Vertex Shader
//=========================================================================================================================
VS {
struct VertexInput
{
    float3 vPosition : POSITION;
    float2 vTexCoord : TEXCOORD0;
};

struct PixelInput
{
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
};

    // Noise function, similar to Unity shader
float hash(float n)
{
    return frac(sin(n) * 43758.5453);
}

float noise(float3 x)
{
    float3 p = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    float n = p.x + p.y * 57.0 + 113.0 * p.z;
    return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
                         lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
                    lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                         lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
}

PixelInput MainVs(VertexInput i)
{
    PixelInput o;
    float3 worldPos = mul(_ObjectToWorld, float4(i.vPosition, 1.0)).xyz;

        // Calculate smear effect based on position and noise
    float3 worldOffset = _Position - _PrevPosition;
    float3 localOffset = worldPos - _Position;
    float dirDot = dot(normalize(worldOffset), normalize(localOffset));

    worldOffset = clamp(worldOffset, -_NoiseHeight, _NoiseHeight);
    worldOffset *= -clamp(dirDot, -1, 0);

    float3 smearOffset = worldOffset * lerp(1, noise(worldPos * _NoiseScale), step(0, _NoiseScale));
    worldPos += smearOffset;

    o.position = mul(_WorldToObject, float4(worldPos, 1.0));
    o.uv = i.vTexCoord;
    return o;
}
}

//=========================================================================================================================
// Pixel Shader
//=========================================================================================================================
PS {
sampler2D _MainTex;

float4 MainPs(PixelInput i) : SV_Target
{
        // Sample the texture and apply the color
    float4 color = tex2D(_MainTex, i.uv) * _Color;

        // Handle material properties (Metallic, Smoothness)
    Material m;
    m.Metalness = _Metallic;
    m.Roughness = 1.0 - _Glossiness;

    return ShadingModelStandard::Shade(i, m);
}
}
