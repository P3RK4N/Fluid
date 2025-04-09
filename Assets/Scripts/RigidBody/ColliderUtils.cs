using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

internal static class ColliderUtils
{
    public static Vector3 ClosestPointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ap = p - a;

        float d1 = Vector3.Dot(ab, ap);
        float d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0f && d2 <= 0f)
            return a; // #1

        Vector3 bp = p - b;
        float d3 = Vector3.Dot(ab, bp);
        float d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3)
            return b; // #2

        Vector3 cp = p - c;
        float d5 = Vector3.Dot(ab, cp);
        float d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6)
            return c; // #3

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f)
        {
            float v = d1 / (d1 - d3);
            return a + v * ab; // #4
        }

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f)
        {
            float v = d2 / (d2 - d6);
            return a + v * ac; // #5
        }

        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
        {
            float v = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return b + v * (c - b); // #6
        }

        float denom = 1f / (va + vb + vc);
        float v0 = vb * denom;
        float w = vc * denom;
        return a + v0 * ab + w * ac; // #0
    }

    public static Vector3 ClosestPointBoxSurface(Vector3 p, Vector3 halfExtents)
    {
        Vector3 clamped = new Vector3(
            Mathf.Clamp(p.x, -halfExtents.x, halfExtents.x),
            Mathf.Clamp(p.y, -halfExtents.y, halfExtents.y),
            Mathf.Clamp(p.z, -halfExtents.z, halfExtents.z)
        );

        if (clamped == p)
        {
            Vector3 d = halfExtents - new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z));

            if (d.x < d.y && d.x < d.z)
                clamped.x = (p.x > 0f) ? halfExtents.x : -halfExtents.x;
            else if (d.y < d.z)
                clamped.y = (p.y > 0f) ? halfExtents.y : -halfExtents.y;
            else
                clamped.z = (p.z > 0f) ? halfExtents.z : -halfExtents.z;
        }

        return clamped;
    }

    public static float SignedDistanceToBox(Vector3 p, Vector3 halfExtents)
    {
        Vector3 q = new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z)) - halfExtents;
        Vector3 maxQ = new Vector3(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f), Mathf.Max(q.z, 0f));

        float outsideDist = maxQ.magnitude;
        float insideDist = Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0f);

        return outsideDist + insideDist;
    }
}

