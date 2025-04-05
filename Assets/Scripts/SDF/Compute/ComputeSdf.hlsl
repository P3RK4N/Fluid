#define COMPUTE_SDF_ENABLED

#define ENABLE_SDF_NORMAL // Stores normals to nearest surfaces in addition to the signed distance
#define ENABLE_SDF_MARGIN // Pushes colliders outwards by a margin (Useful if particles have radius)

float3 _sdfBoundsMin;
float3 _sdfBoundsMax;
uint _sdfResolution;

#ifndef ENABLE_SDF_NORMAL
#   define FieldVal float
#else
#   define FieldVal float4
#endif

RWTexture3D<FieldVal> Field;
Texture3D<FieldVal> FieldTexture;
SamplerState samplerFieldTexture;

float3 _getUVWFromWorld(float3 worldPos)
{
    return (worldPos - _sdfBoundsMin) / (_sdfBoundsMax - _sdfBoundsMin);
}

#define GET_SDF_VALUE(worldPos) FieldTexture.SampleLevel(samplerFieldTexture, _getUVWFromWorld(worldPos), 0)