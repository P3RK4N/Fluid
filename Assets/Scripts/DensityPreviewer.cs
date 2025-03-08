using UnityEngine;

public class DensityPreviewer : Previewer
{
    [SerializeField]
    bool showDifferentials = true;

    [SerializeField]
    float radius = 1.3f;

    [SerializeField]
    int numParticles = 10;

    [SerializeField]
    float gradScale = 0.1f;

    [SerializeField]
    float laplacianScale = 0.1f;

    float e = 1e-6f;

    FluidSim2D sim;

    protected override void preDraw()
    {
        Random.InitState(0);
        sim = new FluidSim2D(numParticles, 20, scale: scale.x);
        sim.step(Time.fixedDeltaTime);
    }

    protected override Color draw(Vector2 coords)
    {
        float t = sim.sampleKernelSumAt(coords);
        return Color.Lerp(Style.DarkColor, Style.LightColor, t);
    }

    void Update()
    {
        radius -= 0.5f * Time.deltaTime;
        OnValidate();
    }

    private void OnDrawGizmos()
    {
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
