using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class ComputeFluidInstancedOverlayRenderer : MonoBehaviour
{
    [EditorOnly] public Material mat;
    [EditorOnly] public Mesh mesh;
    [EditorOnly] public float targetScale = 0.5f;
    public bool debug = true;
    public bool drawDepthOverlay = false;

    ComputeFluidSim sim;
    new BoxCollider collider;

    RenderTexture target;
    RenderTexture targetDepth;
    MaterialPropertyBlock mpb;
    CommandBuffer cmd;

    Vector3 point = new();
    Texture2D copy;

    Camera cam;

    void Awake()
    {
        cam = Camera.main;

        sim = GetComponent<ComputeFluidSim>();
        collider = GetComponent<BoxCollider>();

        target = new RenderTexture
        (
            (int)(targetScale * Screen.width),
            (int)(targetScale * Screen.height),
            0,
            RenderTextureFormat.ARGB32
        );
        target.Create();

        copy = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);

        mpb = new MaterialPropertyBlock();
        cmd = new CommandBuffer { name = "Manual Mesh Render" };
    }

    void Start()
    {
        if (sim == null) return;

        mpb.SetBuffer("positions", sim.positionBuffer);
        mpb.SetBuffer("velocities", sim.velocityBuffer);
    }

    private void OnDestroy()
    {
        cmd?.Release();
    }

    void OnRenderObject()
    {
        if (sim == null || Camera.current != null) return;

        var depthTex = Shader.GetGlobalTexture("_CameraDepthTexture");

        mpb.SetFloat("_ParticleRadius", sim.particleRadius);
        mpb.SetMatrix("invVP", (cam.projectionMatrix * cam.worldToCameraMatrix).inverse);
        mpb.SetFloat("magicFactor", 1.0f / targetScale);
        cmd.Clear();
        cmd.SetupCameraProperties(cam);
        cmd.SetRenderTarget(target);
        cmd.SetGlobalTexture("_CameraDepthTexture", depthTex);
        cmd.ClearRenderTarget(false, true, Color.clear);
        cmd.DrawMeshInstancedProcedural(mesh, 0, mat, 0, sim.numParticles, mpb);
        //cmd.DrawProcedural(Matrix4x4.zero, mat, 0, MeshTopology.Triangles, 6, 1, mpb);

        // Execute it manually
        Graphics.ExecuteCommandBuffer(cmd);

        RenderTexture.active = target;
        copy.ReadPixels(new Rect(0, 0, copy.width, copy.height), 0, 0);
        RenderTexture.active = null;
    }

    private void OnDrawGizmos()
    {
        if (!debug || !Application.isPlaying) return;

        // Drawing bounds
        Vector3Int[] bigMinMax = new Vector3Int[2];
        sim.boundsBuffer.GetData(bigMinMax);

        sim.UpdateBounds();
        Vector3[] minMax = new Vector3[2];
        minMax[0] = bigMinMax[0];
        minMax[1] = bigMinMax[1];
        minMax[0] /= 1000.0f;
        minMax[1] /= 1000.0f;
        var b = new Bounds();
        b.SetMinMax(minMax[0], minMax[1]);

        Gizmos.DrawCube(b.center, b.size);

        MapDepthToWorld();
    }

    private void MapDepthToWorld()
    {
        var pixels = copy.GetRawTextureData<Vector4>();
        
        foreach (var pixel in pixels)
        {
            //Debug.Log(pixel);

            //Gizmos.color = Color.red;
            Gizmos.DrawCube(pixel, Vector3.one * 0.1f);
        }
    }

    void OnGUI()
    {
        // Draw the RenderTexture to screen as a fullscreen overlay
        if (drawDepthOverlay)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), target, ScaleMode.StretchToFill, false);
        }
    }

}
