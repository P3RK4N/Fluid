using UnityEngine;
using static ComputeFluidSim;

[RequireComponent(typeof(ComputeFluidSim))]
public class ComputeFluidTextureRenderer2D : MonoBehaviour
{
    [EditorOnly] public ComputeShader computeShader;
    public Vector2Int resolution;

    ComputeFluidSim sim;

    private RenderTexture renderTexture;

    void Awake()
    {
        InitializeRenderTexture();
        sim = GetComponent<ComputeFluidSim>();
    }

    void Start()
    {
        sim.SetBufferData(computeShader, 1);
    }

    void Update()
    {
        sim.SetUniformData(computeShader);
        DispatchDraw();
    }

    void InitializeRenderTexture()
    {
        renderTexture = new RenderTexture(resolution.x, resolution.y, 0, RenderTextureFormat.ARGB32);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        var rend = GetComponentInChildren<Renderer>();
        if (rend)
        {
            rend.material.mainTexture = renderTexture;
        }

        computeShader.SetTexture(0, "result", renderTexture);
        computeShader.SetTexture(1, "result", renderTexture);
    }

    void DispatchDraw()
    {
        if (computeShader == null || renderTexture == null) return;
        Debug.Assert(sim.dimension == Dimension.Dimension2D, "Invalid dimension for texture renderer!");
        
        computeShader.Dispatch(0, Mathf.CeilToInt((float)(resolution.x * resolution.y) / X), 1, 1);
    }
}
