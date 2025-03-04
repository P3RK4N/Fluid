using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class KernelPreviewer : MonoBehaviour
{
    [SerializeField]
    Vector2Int resolution = new(400, 400);

    [SerializeField]
    Vector2 scale = new(4, 1);

    [SerializeField]
    bool showF = true;

    [SerializeField]
    bool showDF = true;

    [SerializeField]
    float h = 1.4f;

    void OnValidate()
    {
        GetComponent<Renderer>().sharedMaterial.mainTexture = generateTexture();
    }

    private Texture generateTexture()
    {
        Texture2D texture = new Texture2D(resolution.x, resolution.y);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] values = new Color[resolution.x * resolution.y];
        Kernel k = new SphStdKernel(h);

        for (int j = 0; j < resolution.y; j++)
            for (int i = 0; i < resolution.x; i++)
            {
                Vector2 coords = mapToScale(i, j);
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

                values[j * resolution.x + i] = color;
            }

        texture.SetPixels(values);
        texture.Apply();
        return texture;
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

    private Vector2 mapToScale(int i, int j)
    {
        float mappedX = (i / (float)resolution.x - 0.5f) * scale.x;
        float mappedY = (j / (float)resolution.y - 0.5f) * scale.y;
        return new Vector2(mappedX, mappedY);
    }
}
