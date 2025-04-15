using UnityEditor;
using UnityEngine;
using static ComputeFluidSim;

public class DepthInstancedRenderer3D : MonoBehaviour
{
    public Material mat;
    public Mesh mesh;

    RenderParams rp;

    void Awake()
    {
        rp = new RenderParams(mat);
        rp.matProps = new MaterialPropertyBlock();
        rp.camera = Camera.main;
    }

    void OnRenderObject()
    {
        var width = RenderTexture.active.width;
        var height = RenderTexture.active.height;

        rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000);
        rp.matProps.SetInt("width", width);
        rp.matProps.SetInt("height", height);

        Graphics.RenderMeshPrimitives(rp, mesh, 0, width * height);
    }
}
