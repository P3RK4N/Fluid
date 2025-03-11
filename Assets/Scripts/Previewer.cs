using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public abstract class Previewer : MonoBehaviour
{
    [SerializeField]
    public Vector2Int resolution = new(400, 400);

    [SerializeField]
    public Vector2 scale = new(4, 1);

    protected void OnValidate()
    {
        GetComponentInChildren<Renderer>().sharedMaterial.mainTexture = generateTexture();
    }

    private Texture generateTexture()
    {
        Texture2D texture = new Texture2D(resolution.x, resolution.y);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] values = new Color[resolution.x * resolution.y];

        preDraw();
        Parallel.For(0, resolution.y, j =>
        {
            for (int i = 0; i < resolution.x; i++)
            {
                values[j * resolution.x + i] = draw(transformCoords(i, j));
            }
        });
        postDraw();

        texture.SetPixels(values);
        texture.Apply();
        return texture;
    }

    protected virtual Vector2 transformCoords(int x, int y)
    {
        float mappedX = (x / (float)resolution.x - 0.5f) * scale.x;
        float mappedY = (y / (float)resolution.y - 0.5f) * scale.y;
        return new Vector2(mappedX, mappedY);
    }

    protected virtual void preDraw() { }
    protected abstract Color draw(Vector2 coords);
    protected virtual void postDraw() { }
}

