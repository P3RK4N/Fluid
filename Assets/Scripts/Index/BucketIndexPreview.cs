using System.Collections.Generic;
using System.Drawing;
using UnityEditor;
using UnityEngine;


public class BucketIndexPreview : MonoBehaviour
{
    [SerializeField]
    Vector3Int resolution = new Vector3Int(10, 10, 10);

    [SerializeField]
    float radius = 0.5f;

    List<Vector3> randomPoints;
    BucketIndex index;

    void OnValidate()
    {
        index = new BucketIndex(radius, resolution.x, resolution.y, resolution.z);

        initRandomPoints();
        for (int i = 0; i < randomPoints.Count; i++)
        {
            index.put(i, randomPoints[i]);
        }
    }
    
    private void OnDrawGizmos()
    {
        drawGrid();
        drawPoints();
    }

    private void drawPoints()
    {
        Vector3 offset = transform.localPosition;

        // Drawing all random points
        randomPoints.ForEach(p => Gizmos.DrawIcon(offset + p, "", false, Style.DarkColor));

        // Drawing main point
        Vector3 mainPoint = transform.GetChild(0).localPosition;
        Gizmos.DrawIcon(offset + mainPoint, "", false, Style.LightColor);
        Gizmos.color = Style.LightColor;
        Gizmos.DrawWireSphere(offset + mainPoint, radius);

        // Highlighting neighbors
        float radius2 = radius * radius;
        index.ForEachNeighbor(mainPoint, id =>
        {
            var neighbor = randomPoints[id];

            if (Vector3.SqrMagnitude(mainPoint - neighbor) <= radius2)
            {
                Gizmos.DrawIcon(offset + neighbor - Camera.current.transform.forward * 0.03f, "", false, Style.LightColor);
            }
        });
    }

    void drawGrid()
    {
        Vector3 offset = transform.localPosition;

        Vector3 dX = new(radius, 0, 0);
        Vector3 dY = new(0, radius, 0);
        Vector3 dZ = new(0, 0, radius);
        Vector3 idn = new(radius, radius, radius);

        Vector3 bottomBackLeftCenter = offset + 0.5f * idn;

        for (int i = 0; i < resolution.x; i++)
            for (int j = 0; j < resolution.y; j++)
                for (int k = 0; k < resolution.z; k++)
        {
            var center = bottomBackLeftCenter + dZ * k + dX * i + dY * j;
            var bucket = index.getBucket(new Vector3Int(i, j, k));
            var size = bucket.Count > 0 ? idn * 1.03f : idn;
            Gizmos.color = bucket.Count > 0 ? Color.gray : Color.white;
            Gizmos.DrawWireCube(center, size);
        }
    }

    private void initRandomPoints()
    {
        int seed = Random.Range(int.MinValue, int.MaxValue);
        Random.InitState(0);
        Vector3 res = new Vector3(resolution.x, resolution.y, resolution.z);
        randomPoints.Clear();
        for (int i = 0; i < 100; i++)
        {
            var p = Random.insideUnitSphere * 0.5f + Vector3.one * 0.5f;
            p.Scale(res * radius);
            randomPoints.Add(p);
        }
        Random.InitState(seed);
    }
}
