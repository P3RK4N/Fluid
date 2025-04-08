#ifndef COLLIDER_ENABLED
#define COLLIDER_ENABLED

#include "ColliderUtils.hlsl"

struct BoxColliderInfo
{
    float4x4 TR;
    float4x4 inverseTR;
    float4 scale;
    float4 velocity;
    float4 angularVelocity;
    int4 force;
    int4 torque;
};

uint _NumBoxColliders;
float _ParticleRadius;
float _RestitutionCoeff;
float _DampingCoeff;
float _ForceCoeff;
float _TorqueCoeff;
int _FeedbackResolution;
RWStructuredBuffer<BoxColliderInfo> _BoxColliders;


void _resolveCollisions(inout float3 worldPos, inout float3 worldVel)
{
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
            float3 reflectedRelativeVel = _RestitutionCoeff * reflect(relativeVel, normalizedWorldNormal);

            // Update particle values
            worldVel = reflectedRelativeVel + boxPointVelocity;
            worldPos = closestPointWorld;

            /*
                ################################################
                ############ RIGIDBODY FEEDBACK ################
                ################################################
            */


            float normalVel = dot(relativeVel, normalizedWorldNormal);

            if (normalVel >= 0.0f) continue;

            float3 normalVelDir = normalVel * normalizedWorldNormal;

            // Impulse and damping ~ velocity at collision
            float3 force = (_DampingCoeff - _ForceCoeff * -sd) * normalVelDir;

            // Apply torque
            float3 torque = _TorqueCoeff * cross(r, force);

            InterlockedAdd(_BoxColliders[i].force.x, int(_FeedbackResolution * force.x));
            InterlockedAdd(_BoxColliders[i].force.y, int(_FeedbackResolution * force.y));
            InterlockedAdd(_BoxColliders[i].force.z, int(_FeedbackResolution * force.z));

            InterlockedAdd(_BoxColliders[i].torque.x, int(_FeedbackResolution * torque.x));
            InterlockedAdd(_BoxColliders[i].torque.y, int(_FeedbackResolution * torque.y));
            InterlockedAdd(_BoxColliders[i].torque.z, int(_FeedbackResolution * torque.z));
        }
    }
}

#define RESOLVE_COLLIDER_COLLISIONS(worldPos, worldVel) _resolveCollisions(worldPos, worldVel);

#endif