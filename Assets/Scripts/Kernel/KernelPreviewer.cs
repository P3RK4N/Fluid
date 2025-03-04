using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class KernelPreviewer : Previewer
{
    [SerializeField]
    bool showF = true;

    [SerializeField]
    bool showDF = true;

    [SerializeField]
    float h = 1.4f;

    Kernel k;

    protected override void preDraw()
    {
        k = new SphStdKernel(h);
    }

    protected override Color draw(Vector2 coords)
    {
        float res = k.F(coords.x);
        float dRes = k.dF(coords.x);

        Color color = Style.DarkColor;
        if (showF && shouldPlot(coords.y, res))
        {
            color.r = 1.0f;
        }
        if (showDF && shouldPlot(coords.y, dRes))
        {
            color.g = 1.0f;
        }
        if (coords.x == 0.0f || coords.y == 0.0f)
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
