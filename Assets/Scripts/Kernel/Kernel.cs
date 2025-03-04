using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

abstract class Kernel
{
    public abstract float F(float distance);
    public abstract float dF(float distance);
    public abstract float ddF(float distance);
}
