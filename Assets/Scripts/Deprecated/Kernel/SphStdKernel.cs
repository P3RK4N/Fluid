using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SphStdKernel : Kernel
{
    private float h;
    private float h2;
    private float h3;
    private float h4;
    private float h5; 

    public SphStdKernel(float h)
    {
        this.h = h;
        this.h2 = h * h;
        this.h3 = h * h * h;
        this.h4 = h * h * h * h;
        this.h5 = h * h * h * h * h;
    }

    public override float d2F(float distance)
    {
        if (distance >= h)
        {
            return 0;
        }

        var distance2 = distance * distance;
        var x = distance2 / h2;
        return 945.0f / (32.0f * Mathf.PI * h5) * (1f - x) * (3f * x - 1f);
    }

    public override float dF(float distance)
    {
        if (distance >= h)
        {
            return 0;
        }

        var distance2 = distance * distance;
        var x = 1.0f - distance2 / h2;
        return -945.0f / (32.0f * (float)Mathf.PI * h5) * distance * x * x;
    }

    public override float F(float distance)
    {
        if (distance >= h)
        {
            return 0;
        }

        var distance2 = distance * distance;
        var x = 1.0f - distance2 / h2;
        return 315.0f / (64.0f * (float)Math.PI * h3) * x * x * x;
    }

    public override Vector2 grad(float distance, Vector2 normalizedDirectionToCenter)
    {
        return -dF(distance) * normalizedDirectionToCenter;
    }

    public override Vector3 grad(float distance, Vector3 normalizedDirectionToCenter)
    {
        return -dF(distance) * normalizedDirectionToCenter;
    }
}
