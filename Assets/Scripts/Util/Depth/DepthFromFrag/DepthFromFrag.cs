using UnityEngine;
using UnityEngine.Rendering;

public class DepthFromFrag : MonoBehaviour
{
    [EditorOnly] public Mesh mesh;
    [EditorOnly] public Material mat;

    RenderParams rp;
    CommandBuffer cmd;

    private void Awake()
    {
        cmd = new();

        rp = new RenderParams(mat);
        rp.camera = Camera.main;
        rp.matProps = new MaterialPropertyBlock();
        rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000);
    }

    void OnRenderObject()
    {
        rp.matProps.SetMatrix("invVP", (Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix).inverse);
        rp.matProps.SetMatrix("TRS", transform.localToWorldMatrix);

        cmd.Clear();
        //cmd.SetupCameraProperties(Camera.main);
        //cmd.SetRenderTarget(Camera.main.targetTexture);
        cmd.DrawMesh(mesh, transform.localToWorldMatrix, mat, 0, 0, rp.matProps);
        Graphics.ExecuteCommandBuffer(cmd);
    }
}
