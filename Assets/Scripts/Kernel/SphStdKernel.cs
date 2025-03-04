using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class SphStdKernel : Kernel
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

    public override float ddF(float distance)
    {
        throw new NotImplementedException();
    }

    public override float dF(float distance)
    {
        var distance2 = distance * distance;

        if (distance2 > h2)
        {
            return 0;
        }

        var x = 1.0f - distance2 / h2;
        return -315.0f * 6 * distance / (64.0f * (float)Math.PI * h5) * x * x;
    }

    public override float F(float distance)
    {
        var distance2 = distance * distance;

        if (distance2 > h2)
        {
            return 0;
        }

        var x = 1.0f - distance2 / h2;
        return 315.0f / (64.0f * (float)Math.PI * h3) * x * x * x;
    }
}
