#ifndef COLLIDER_UTILS_ENABLED
#define COLLIDER_UTILS_ENABLED

/*
    #######################################################
    ####################### UTILS #########################
    #######################################################
*/

/*
    Closest point on triangle
    https://stackoverflow.com/questions/2924795/fastest-way-to-compute-point-to-triangle-distance-in-3d
*/
float3 closestPointTriangle(in float3 p, in float3 a, in float3 b, in float3 c)
{
    float3 ab = b - a;
    float3 ac = c - a;
    float3 ap = p - a;

    float d1 = dot(ab, ap);
    float d2 = dot(ac, ap);
    if (d1 <= 0.0f && d2 <= 0.0f)
        return a; // #1

    float3 bp = p - b;
    float d3 = dot(ab, bp);
    float d4 = dot(ac, bp);
    if (d3 >= 0.0f && d4 <= d3)
        return b; // #2

    float3 cp = p - c;
    float d5 = dot(ab, cp);
    float d6 = dot(ac, cp);
    if (d6 >= 0.0f && d5 <= d6)
        return c; // #3

    float vc = d1 * d4 - d3 * d2;
    if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
    {
        float v = d1 / (d1 - d3);
        return a + v * ab; // #4
    }

    float vb = d5 * d2 - d1 * d6;
    if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
    {
        float v = d2 / (d2 - d6);
        return a + v * ac; // #5
    }

    float va = d3 * d6 - d5 * d4;
    if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
    {
        float v = (d4 - d3) / ((d4 - d3) + (d5 - d6));
        return b + v * (c - b); // #6
    }

    float denom = 1.0f / (va + vb + vc);
    float v = vb * denom;
    float w = vc * denom;
    return a + v * ab + w * ac; // #0
}

/*
    Closest point on box surface
*/
float3 closestPointBoxSurface(float3 p, float3 halfExtents)
{
    // Clamp to box bounds
    float3 clamped = clamp(p, -halfExtents, halfExtents);

    // If inside box (no clamping happened), project to nearest face
    if (all(clamped == p))
    {
        float3 d = halfExtents - abs(p);
        
        if (d.x < d.y && d.x < d.z)
            clamped.x = (p.x > 0) ? halfExtents.x : -halfExtents.x;
        else if (d.y < d.z)
            clamped.y = (p.y > 0) ? halfExtents.y : -halfExtents.y;
        else
            clamped.z = (p.z > 0) ? halfExtents.z : -halfExtents.z;
    }

    return clamped;
}

/*
    Signed distance to the box
    https://iquilezles.org/articles/distfunctions/
*/
float sdBox(float3 p, float3 halfExtents)
{
    float3 q = abs(p) - halfExtents;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

#endif