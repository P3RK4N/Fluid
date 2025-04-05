using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class DensityPreviewer : Previewer
{
    [SerializeField]
    public bool showDifferentials = true;

    [SerializeField]
    public float radius = 1.3f;

    [SerializeField]
    public int numParticles = 10;

    [SerializeField]
    public float densityScale = 1.0f;

    [SerializeField]
    public float gradScale = 0.1f;

    [SerializeField]
    public float laplacianScale = 0.1f;

    [SerializeField]
    public float deltaTime = 0.001f;

    public FluidSim2D sim;

    protected override void preDraw()
    {
        //Random.InitState(0);
        //sim = new FluidSim2D(numParticles, 20, radius, scale: scale.x);
        //sim.step(0);
    }

    protected override Color draw(Vector2 coords)
    {
        float t = sim.sampleKernelSumAt(coords);
        return Color.Lerp(Style.DarkColor, Style.LightColor, t * densityScale);
    }

    private void Update()
    {
        if (sim == null)
        {
            Random.InitState(0);
            sim = new FluidSim2D(numParticles, 20, radius, scale: scale.x);
            sim.step(0);
            OnValidate();
        }

    }

    private void OnDrawGizmos()
    {
        if (sim == null) return;

        if (showDifferentials)
        {
            for (float i = 0; i < 1.0f; i += 0.1f)
                for (float j = 0; j < 1.0f; j += 0.1f)
            {
                Vector2 coords = new(i, j);
                
                Vector2 dir = sim.sampleGradientAt(coords * scale.x);
                Gizmos.DrawLine(coordToWorldPos(coords), coordToWorldPos(coords + dir * gradScale));
                
                float val = sim.sampleLaplacianAt(coords * scale.x);
                Gizmos.DrawIcon(coordToWorldPos(coords), "", true, Color.Lerp(Style.DarkColor, Style.LightColor, val * laplacianScale));
            }
        }
    }

    protected override Vector2 transformCoords(int x, int y) 
    {
        return new Vector2((float)x / resolution.x * scale.x, (float)y / resolution.y * scale.x);
    }

    private Vector3 coordToWorldPos(Vector2 coord)
    {
        Vector3 localPosition = new Vector3(0, coord.y, coord.x - 0.5f); // Y up, Z right

        // Apply object transform (scale, rotation, position)
        return transform.TransformPoint(localPosition);
    }
}


[CustomEditor(typeof(DensityPreviewer))]
[CanEditMultipleObjects]
public class DensityPreviewerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DensityPreviewer t = target as DensityPreviewer;


        if (GUILayout.Button("Reinit"))
        {
            Random.InitState(0);
            t.sim = new FluidSim2D(t.numParticles, 20, t.radius, scale: t.scale.x);
            t.sim.step(0);
        }

        if (GUILayout.Button("Step"))
        {
            t.sim.step(t.deltaTime);
            Debug.Log("Stepped");
        }

        DrawDefaultInspector();
    }
}