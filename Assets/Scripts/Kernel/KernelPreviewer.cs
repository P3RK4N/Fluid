using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class KernelPreviewer : Previewer
{
    [SerializeField]
    Kernel.KernelType type = Kernel.KernelType.SphStdKernel;

    [SerializeField]
    bool showF = true;

    [SerializeField]
    bool showDF = true;

    [SerializeField]
    bool showD2F = true;

    [SerializeField]
    float h = 1.4f;

    Kernel k;

    protected override void preDraw()
    {
        switch (type)
        {
            case Kernel.KernelType.SphStdKernel:
                k = new SphStdKernel(h);
                break;
            case Kernel.KernelType.SphSpikyKernel:
                k = new SphSpikyKernel(h);
                break;
        }
    }

    protected override Color draw(Vector2 coords)
    {
        float res = k.F(Mathf.Abs(coords.x));
        float dRes = k.dF(Mathf.Abs(coords.x));
        float d2Res = k.d2F(Mathf.Abs(coords.x));

        Color color = Style.DarkColor;
        if (showF && shouldPlot(coords.y, res))
        {
            color.r = 1.0f;
        }
        if (showDF && shouldPlot(coords.y, dRes))
        {
            color.g = 1.0f;
        }
        if (showD2F && shouldPlot(coords.y, d2Res))
        {
            color.b = 1.0f;
        }
        if (coords.x == 0.0f || coords.y == 0.0f || Mathf.Abs(coords.y - Mathf.Round(coords.y)) < 0.01f)
        {
            color = Color.white;
        }

        return color;
    }

    protected override void postDraw()
    {
        
    }

    private bool shouldPlot(float y, float res)
    {
        // Different prefix
        if (y * res < 0.0f)
        {
            return false;
        }

        return Mathf.Abs(y) < Mathf.Abs(res);
    }
}
