using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class Kernel<Vector>
{
    public abstract float F(float distance);
    public abstract float dF(float distance);
    public abstract float d2F(float distance);
    public abstract Vector grad(float distance, Vector normalizedDirectionToCenter);
}
