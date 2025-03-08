using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public abstract class Kernel
{
    public enum KernelType
    {
        SphStdKernel,
        SphSpikyKernel
    }

    public abstract float F(float distance);
    public abstract float dF(float distance);
    public abstract float d2F(float distance);
    public abstract Vector2 grad(float distance, Vector2 normalizedDirectionToCenter);
    public abstract Vector3 grad(float distance, Vector3 normalizedDirectionToCenter);
}
