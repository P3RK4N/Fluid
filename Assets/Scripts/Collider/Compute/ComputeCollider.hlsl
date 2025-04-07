#ifndef COLLIDER_ENABLED
#define COLLIDER_ENABLED

#include "ColliderUtils.hlsl"

struct BoxColliderInfo
{
    float4x4 TR;
    float4x4 inverseTR;
    float4 scale;
    int4 force;
    int4 torque;
};

uint _NumBoxColliders;
float _ParticleRadius;
float _RestitutionCoeff;
RWStructuredBuffer<BoxColliderInfo> _BoxColliders;


float _resolveCollisions(inout float3 worldPos, inout float3 worldVel)
{
    float retval = 0.0f;

    for (uint i = 0; i < _NumBoxColliders; i++)
    {
        BoxColliderInfo box = _BoxColliders[i];

        // Transform world position to box local space
        float3 localPos = mul(box.inverseTR, float4(worldPos, 1.0)).xyz;

        float3 halfExtents = box.scale.xyz / 2.0f + _ParticleRadius;

        // Compute signed distance from the box surface
        float sd = sdBox(localPos, halfExtents);

        // If we're intersecting or penetrating the box (subtract epsilon to avoid zero normals)
        if (sd < -1e-6)
        {
            // Find the closest point on the box surface in local space
            float3 closestPointLocal = closestPointBoxSurface(localPos, halfExtents);

            float3 closestPointWorld = mul(box.TR, float4(closestPointLocal, 1.0f)).xyz;

            float3 worldNormal = worldPos - closestPointWorld;
            float3 normalizedWorldNormal = normalize(worldNormal);

            worldVel = _RestitutionCoeff * reflect(worldVel, normalizedWorldNormal);
            worldPos = closestPointWorld;

            retval = sd;
        }
    }

    return retval;
}

#define RESOLVE_COLLIDER_COLLISIONS(worldPos, worldVel) _resolveCollisions(worldPos, worldVel);

#endif