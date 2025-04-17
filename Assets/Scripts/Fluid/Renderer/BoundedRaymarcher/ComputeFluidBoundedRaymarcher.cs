using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class ComputeFluidBoundedRaymarcher : MonoBehaviour
{
    [EditorOnly] public Material mat;
    [EditorOnly] public float targetScale = 0.5f;
    public float step = 1.0f;
    public float surfaceThreshold = 200.0f;
    public float densityCoeff = 0.001f;
    public bool debug = true;
    public bool drawDepthOverlay = false;

    ComputeFluidSim sim;
    ComputeGrid grid;

    RenderTexture target;
    Texture2D copy;
    MaterialPropertyBlock mpb;
    CommandBuffer cmd;

    NativeArray<Color> depthData;

    void Awake()
    {
        sim = GetComponent<ComputeFluidSim>();
        grid = GetComponent<ComputeGrid>();

        target = new RenderTexture
        (
            (int)(targetScale * Screen.width),
            (int)(targetScale * Screen.height),
            0,
            RenderTextureFormat.ARGBFloat
        );
        target.Create();

        copy = new Texture2D(target.width, target.height, TextureFormat.RGBAFloat, false);

        mpb = new MaterialPropertyBlock();
        cmd = new CommandBuffer { name = "Manual Mesh Render" };
    }

    void Start()
    {
        if (sim == null) return;

        mpb.SetBuffer("bounds", sim.boundsBuffer);
        mpb.SetBuffer("positions", sim.predictedPositionBuffer);
        mpb.SetBuffer("densities", sim.densityBuffer);
        grid.Bind(ref mpb);
    }

    private void OnDestroy()
    {
        cmd?.Release();
    }

    void OnRenderObject()
    {
        if (sim == null || Camera.current != null) return;

        var depthTex = Shader.GetGlobalTexture("_CameraDepthTexture");

        mpb.SetMatrix("invVP", (Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix).inverse);
        mpb.SetFloat("step", Mathf.Max(step, 0.01f));
        mpb.SetFloat("densityCoeff", densityCoeff);
        mpb.SetFloat("surfaceThreshold", surfaceThreshold);

        cmd.Clear();
        cmd.SetupCameraProperties(Camera.main);
        cmd.SetRenderTarget(target);
        cmd.SetGlobalTexture("_CameraDepthTexture", depthTex);
        cmd.ClearRenderTarget(false, true, Color.clear);
        cmd.DrawProcedural(Matrix4x4.identity, mat, 0, MeshTopology.Triangles, 6, 1, mpb);

        // Execute it manually
        Graphics.ExecuteCommandBuffer(cmd);

        RenderTexture.active = target;
        copy.ReadPixels(new Rect(0, 0, copy.width, copy.height), 0, 0);
        depthData = copy.GetRawTextureData<Color>();
    }

    public (Vector3 min, Vector3 max) GetBounds()
    {
        Vector3Int[] bigMinMax = new Vector3Int[2];
        sim.boundsBuffer.GetData(bigMinMax);
        Vector3[] minMax = new Vector3[2];
        minMax[0] = bigMinMax[0];
        minMax[1] = bigMinMax[1];
        minMax[0] /= 1000.0f;
        minMax[1] /= 1000.0f;

        return (minMax[0], minMax[1]);
    }

    private void OnDrawGizmos()
    {
        if (!debug || !Application.isPlaying) return;

        VisualizeRaymarchRanges();
    }

    public static bool RayIntersectsAABB(Vector3 rayOrigin, Vector3 rayDir, Vector3 boxMin, Vector3 boxMax, out float tmin, out float tmax)
    {
        tmin = float.NegativeInfinity;
        tmax = float.PositiveInfinity;

        for (int i = 0; i < 3; i++)
        {
            float origin = rayOrigin[i];
            float dir = rayDir[i];
            float min = boxMin[i];
            float max = boxMax[i];

            if (Mathf.Abs(dir) < 1e-5f)
            {
                if (origin < min || origin > max)
                    return false;
            }
            else
            {
                float t1 = (min - origin) / dir;
                float t2 = (max - origin) / dir;

                if (t1 > t2)
                    (t1, t2) = (t2, t1);

                tmin = Mathf.Max(tmin, t1);
                tmax = Mathf.Min(tmax, t2);

                if (tmin > tmax)
                    return false;
            }
        }

        return true;
    }

    private void VisualizeRaymarchRanges()
    {
        if (depthData == null)
        {
            return;
        }

        (var minn, var maxx) = GetBounds();
        var invPV = (Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix).inverse;

        for (int i = 0; i < target.height; i++)
            for (int j = 0; j < target.width; j++)
            {
                // Read and normalize 8-bit depth (you can adjust this if using float depth)
                float depth = 1 - depthData[i * target.width + j].r;

                // Convert pixel to normalized screen UVs [0, 1]
                Vector2 screenUV = new Vector2((j + 0.5f) / target.width, (i + 0.5f) / target.height);

                // Convert to NDC [-1, 1]
                Vector2 ndc = screenUV * 2.0f - Vector2.one;

                // Reconstruct world-space T0 and T1
                Vector4 nearClipPos = new Vector4(ndc.x, ndc.y, -1, 1.0f);
                Vector4 farClipPos = new Vector4(ndc.x, ndc.y, depth * 2 - 1, 1.0f);

                Vector4 worldNearH = invPV * nearClipPos;
                Vector4 worldFarH = invPV * farClipPos;

                Vector3 worldNear = worldNearH / worldNearH.w;
                Vector3 worldFar = worldFarH / worldFarH.w;

                // Clamp ray segment to your bounding box if needed
                var dir = (worldFar - worldNear).normalized;
                float tdepth = (worldFar - worldNear).magnitude;
                float tmin, tmax;

                if (RayIntersectsAABB(worldNear, dir, minn, maxx, out tmin, out tmax)) // Ensure within AABB
                {
                    tmin = Mathf.Max(tmin, 0); // Ensure start from near plane
                    tmax = Mathf.Min(tmax, tdepth); // ensure end before depth

                    if (tmin < tmax) // Ensure existing range
                    {
                        // For now, just draw the ray segment
                        Debug.DrawLine(worldNear + tmin * dir, worldNear + tmax * dir, Color.cyan, 0, false);
                    }
                }
            }
    }

    void OnGUI()
    {
        // Draw the RenderTexture to screen as a fullscreen overlay
        if (drawDepthOverlay)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), target, ScaleMode.ScaleToFit, false);
        }
    }

}
