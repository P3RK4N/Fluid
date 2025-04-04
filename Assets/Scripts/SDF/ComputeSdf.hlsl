#define COMPUTE_SDF_ENABLED

// Enable this for now (stores normals to nearest surfaces instead of signed distances)
#define ENABLE_SDF_NORMAL

float3 _sdfBoundsMin;
float3 _sdfBoundsMax;
int _sdfResolution;

#ifndef ENABLE_SDF_NORMAL
#   define FieldVal float
    RWTexture3D<float> Field;
    Texture3D<float> FieldTexture;
#else
#   define FieldVal float4
    RWTexture3D<float4> Field;
    Texture3D<float4> FieldTexture;
#endif

SamplerState samplerFieldTexture;

float3 _getUVWFromWorld(float3 worldPos)
{
    return (worldPos - _sdfBoundsMin) / (_sdfBoundsMax - _sdfBoundsMin);
}

#define GET_SDF_VALUE(worldPos) FieldTexture.SampleLevel(samplerFieldTexture, _getUVWFromWorld(worldPos), 0)