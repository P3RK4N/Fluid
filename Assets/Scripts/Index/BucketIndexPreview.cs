using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;


public class BucketIndexPreview : MonoBehaviour
{
    [SerializeField]
    Vector3Int resolution = new Vector3Int(10, 10, 10);

    [SerializeField]
    float radius = 0.5f;

    [SerializeField]
    Vector3 point;

    List<Vector3> randomPoints;

    BucketIndex index;

    void OnValidate()
    {
        index = new BucketIndex(radius, resolution.x, resolution.y, resolution.z);
        initRandomPoints();
        index.put(-1, point);

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

        foreach (var point in randomPoints)
        {
            Gizmos.DrawIcon(offset + point, "", false, Color.red);
        }

        float radius2 = radius * radius;

        Gizmos.DrawIcon(offset + point, "", false, Color.blue);

        index.ForEachNeighbor(point, id =>
        {
            if (id == -1) return;

            var neighbor = randomPoints[id];

            if (Vector3.SqrMagnitude(point - neighbor) <= radius2)
            {
                Gizmos.DrawIcon(offset + neighbor, "", false, Color.green);
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
            var size = bucket.Count > 0 ? idn * 1.05f : idn;
            Gizmos.color = bucket.Count > 0 ? Color.gray : Color.gray;

            Gizmos.DrawWireCube(center, size);
        }
    }

    private void initRandomPoints()
    {
        int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        UnityEngine.Random.InitState(0);
        Vector3 res = new Vector3(resolution.x, resolution.y, resolution.z);
        randomPoints.Clear();
        for (int i = 0; i < 100; i++)
        {
            var p = UnityEngine.Random.insideUnitSphere * 0.5f + Vector3.one * 0.5f;
            p.Scale(res * radius);
            randomPoints.Add(p);
        }
        UnityEngine.Random.InitState(seed);
    }
}
