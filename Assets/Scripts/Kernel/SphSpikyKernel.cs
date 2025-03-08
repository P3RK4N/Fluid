using UnityEngine;

public class SphSpikyKernel : Kernel
{
    private float h;
    private float h2;
    private float h3;
    private float h4;
    private float h5;

    public SphSpikyKernel(float h)
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

        var x = 1.0f - distance / h;
        return 90.0f / (Mathf.PI * h5) * x;
    }

    public override float dF(float distance)
    {
        if (distance >= h)
        {
            return 0;
        }

        var x = 1.0f - distance / h;
        return -45.0f / (Mathf.PI * h4) * x * x;
    }

    public override float F(float distance)
    {
        if (distance >= h)
        {
            return 0;
        }

        var x = 1.0f - distance / h;
        return 15.0f / (Mathf.PI * h3) * x * x * x;
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
