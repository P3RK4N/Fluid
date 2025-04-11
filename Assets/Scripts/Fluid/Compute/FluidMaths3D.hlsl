#ifndef FLUID_MATHS_ENABLED
#define FLUID_MATHS_ENABLED

#define PI 3.14159265359f

struct _FluidMaths
{
    float SmoothingKernelPoly6(float dst, float radius)
    {
	    if (dst < radius)
	    {
		    float scale = 315 / (64 * PI * pow(abs(radius), 9));
		    float v = radius * radius - dst * dst;
		    return v * v * v * scale;
	    }
	    return 0;
    }

    float SmoothingKernelPoly6Smooth(float dst, float radius)
    {
        if (dst < radius)
        {
            float scale = 315.0 / (64.0 * PI * pow(abs(radius), 9.0));
            float v = radius * radius - dst * dst;
            return pow(v, 2.0) * scale * radius; // modified power and compensating scale
        }
        return 0.0;
    }

    float SpikyKernelPow3(float dst, float radius)
    {
	    if (dst < radius)
	    {
		    float scale = 15 / (PI * pow(radius, 6));
		    float v = radius - dst;
		    return v * v * v * scale;
	    }
	    return 0;
    }

    float SpikyKernelPow2(float dst, float radius)
    {
	    if (dst < radius)
	    {
		    float scale = 15 / (2 * PI * pow(radius, 5));
		    float v = radius - dst;
		    return v * v * scale;
	    }
	    return 0;
    }

    float DerivativeSpikyPow3(float dst, float radius)
    {
	    if (dst <= radius)
	    {
		    float scale = 45 / (pow(radius, 6) * PI);
		    float v = radius - dst;
		    return -v * v * scale;
	    }
	    return 0;
    }

    float DerivativeSpikyPow2(float dst, float radius)
    {
	    if (dst <= radius)
	    {
		    float scale = 15 / (pow(radius, 5) * PI);
		    float v = radius - dst;
		    return -v * scale;
	    }
	    return 0;
    }

    float DensityKernel(float dst, float radius)
    {
	    //return SmoothingKernelPoly6(dst, radius);
	    return SpikyKernelPow2(dst, radius);
    }

    float NearDensityKernel(float dst, float radius)
    {
	    return SpikyKernelPow3(dst, radius);
    }

    float DensityDerivative(float dst, float radius)
    {
	    return DerivativeSpikyPow2(dst, radius);
    }

    float NearDensityDerivative(float dst, float radius)
    {
	    return DerivativeSpikyPow3(dst, radius);
    }

    float ViscosityKernel(float dst, float radius)
    {
	    return SmoothingKernelPoly6(dst, radius);
    }

    float RigidBodyKernel(float dst, float radius)
    {
        return SmoothingKernelPoly6Smooth(dst, radius);
    }
};

_FluidMaths FluidMaths;

#endif