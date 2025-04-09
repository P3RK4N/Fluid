#ifndef COLLIDER_ENABLED
#define COLLIDER_ENABLED

#include "ColliderUtils.hlsl"

struct RigidBodyInfo
{
    float4x4 TR;
    float4x4 inverseTR;
    float4 scale;
    float4 velocity;
    float4 angularVelocity;
    int4 accumulatedForce;
    int4 accumulatedTorque;
};

int3 _RbOffsets;
RWStructuredBuffer<RigidBodyInfo> _RigidBodyInfos;

void _resolveCollisions(inout float3 worldPos, inout float3 worldVel, float radius, float restitution)
{
    // Boxes
    for (uint i = 0; i < _RbOffsets.x; i++)
    {
        RigidBodyInfo box = _RigidBodyInfos[i];

        // Transform world position to box local space
        float3 localPos = mul(box.inverseTR, float4(worldPos, 1.0)).xyz;

        float3 halfExtents = box.scale.xyz / 2.0f + radius;

        // Compute signed distance from the box surface
        float sd = sdBox(localPos, halfExtents);

        // If we're intersecting or penetrating the box (subtract epsilon to avoid zero normals)
        if (sd < -1e-6)
        {
            // Find the closest point on the box surface in local space and world space
            float3 closestPointLocal = closestPointBoxSurface(localPos, halfExtents);
            float3 closestPointWorld = mul(box.TR, float4(closestPointLocal, 1.0f)).xyz;

            // Compute world normal
            float3 worldNormal = worldPos - closestPointWorld;
            float3 normalizedWorldNormal = -normalize(worldNormal); // Needs flip because we inside

            // Compute box point velocity at contact point
            float3 r = closestPointWorld - box.TR[3].xyz; // Vector from center to contact
            float3 boxPointVelocity = box.velocity + cross(box.angularVelocity, r);

            // Compute relative velocity
            float3 relativeVel = worldVel - boxPointVelocity;
            float3 reflectedRelativeVel = restitution * reflect(relativeVel, normalizedWorldNormal);

            // Update particle values
            worldVel = reflectedRelativeVel + boxPointVelocity;
            worldPos = closestPointWorld;
        }
    }

    // Spheres not implemented (russian Assert)
    for (uint i = _RbOffsets.x; i < _RbOffsets.y; i++)
    {
        worldPos = (float3) 0;
        worldVel = (float3) 0;
    }

    // Meshes not implemented (russian Assert)
    for (uint i = _RbOffsets.x; i < _RbOffsets.y; i++)
    {
        worldPos = (float3) 0;
        worldVel = (float3) 0;
    }
}

#define RESOLVE_COLLIDER_COLLISIONS(worldPos, worldVel, particleRadius, restitutionCoeff) _resolveCollisions(worldPos, worldVel, particleRadius, restitutionCoeff);

#endif