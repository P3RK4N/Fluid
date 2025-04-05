using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;

public class BucketIndexPreviewer : Previewer
{
    [SerializeField]
    bool useIndex = false;

    [SerializeField]
    int randomPointsCount = 50;

    [SerializeField]
    int gridResolution = 20;

    [SerializeField]
    float radius = 0.1f;

    [SerializeField]
    float thickness = 0.001f;

    [SerializeField]
    Vector2 center = Vector2.zero;

    List<Vector2> randomPoints;
    BucketIndex<Vector2> index;
    float ex;
    float ey;
    float e;
    float radius2;

    bool initialized = false;

    protected override void preDraw()
    {
        if (!initialized)
        {
            initialized = true;
            initRandomPoints();
        }

        radius2 = radius * radius;
        ex = 1.0f / resolution.x * radius * gridResolution;
        ey = 1.0f / resolution.y * radius * gridResolution;
        e = Mathf.Max(ex, ey);
    }

    protected override Color draw(Vector2 coords)
    {
        // Color grid and highlight center neighbors
        var inverseCoords = coords / radius;
        
        var ix = Mathf.Abs(Mathf.Round(inverseCoords.x) - inverseCoords.x);
        var iy = Mathf.Abs(Mathf.Round(inverseCoords.y) - inverseCoords.y);

        if (ix < thickness || iy < thickness)
        {
            var inverseCenter = center / radius;
            bool highlight =
                Mathf.Abs(Mathf.Floor(inverseCenter.x) + 0.5f - inverseCoords.x) <= 1.5 &&
                Mathf.Abs(Mathf.Floor(inverseCenter.y) + 0.5f - inverseCoords.y) <= 1.5;

            return highlight ? Color.white : Color.gray;
        }

        float distance2 = Vector2.SqrMagnitude(coords - center);
        bool inside = distance2 <= radius2;
        Color pixel = Style.DarkColor;

        // Color center circle
        if (Mathf.Abs(distance2 - radius2) < 5.0f * e)
        {
            return Style.LightColor;
        }

        // Color random points and highlight center neighbors
        if (useIndex)
        {
            index.ForEachNeighbor(coords, i =>
            {
                var point = randomPoints[i];
                if (Vector2.SqrMagnitude(coords - point) < e)
                {
                    pixel = inside ? Style.LightColor : Color.gray;
                }
            });
        }
        else
        {
            for (int i = 0; i < randomPointsCount; i++)
            {
                var point = randomPoints[i];
                if (Vector2.SqrMagnitude(coords - point) < e)
                {
                    pixel = inside ? Style.LightColor : Color.gray;
                    break;
                }
            }
        }

        return pixel;
    }

    public static int Mod(int a, int n) => (a % n + n) % n;

    protected override Vector2 transformCoords(int x, int y)
    {
        return new Vector2(x / (float)resolution.x * radius * gridResolution, y / (float)resolution.y * radius * gridResolution);
    }

    public void initRandomPoints()
    {
        randomPoints = new List<Vector2>(randomPointsCount);
        index = new(radius, gridResolution, gridResolution);

        int seed = Random.Range(int.MinValue, int.MaxValue);
        Random.InitState(0);
        Vector3 res = Vector3.one * gridResolution;
        for (int i = 0; i < randomPointsCount; i++)
        {
            var p = Random.insideUnitSphere * 0.5f + Vector3.one * 0.5f;
            p.Scale(res * radius);
            index.put(randomPoints.Count, p);
            randomPoints.Add(p);
        }
        Random.InitState(seed);
    }
}

[CustomEditor(typeof(BucketIndexPreviewer))]
[CanEditMultipleObjects]
public class BucketIndexPreviewerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BucketIndexPreviewer t = target as BucketIndexPreviewer;


        if (GUILayout.Button("Reinit"))
        {
            t.initRandomPoints();
        }

        DrawDefaultInspector();
    }
}