using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class BucketIndex
{
    private int resX;
    private int resY;
    private int resZ;
    private float radius;
    private float inverseRadius;

    Dictionary<int, int4> reverseIndex;
    List<int>[,,] index;

    public BucketIndex(float radius, int resX, int resY, int resZ)
    {
        this.radius = radius;
        this.inverseRadius = 1.0f / radius;
        this.resX = resX;
        this.resY = resY;
        this.resZ = resZ;

        index = new List<int>[resX, resY, resZ];
        reverseIndex = new();
    }

    public void put(int id, Vector3 position)
    {
        Vector3Int bucketCoords = getBucketCoords(position);
        var bucket = getBucket(bucketCoords);
        var elementIndex = bucket.Count;

        bucket.Add(id);
        reverseIndex.Add(id, new int4(bucketCoords.x, bucketCoords.y, bucketCoords.z, elementIndex));
    }

    public void update(int id, Vector3 position)
    {
        remove(id);
        put(id, position);
    }

    public void remove(int id)
    {
        var indices = reverseIndex[id];
        reverseIndex.Remove(id);

        var bucket = index[indices.x, indices.y, indices.z];
        int elementIndex = indices.w;

        // Swap and pop
        bucket[elementIndex] = bucket[bucket.Count - 1];
        bucket.RemoveAt(bucket.Count - 1);
    }

    public List<int> getBucket(Vector3Int bucketCoords)
    {
        if (index[bucketCoords.x, bucketCoords.y, bucketCoords.z] == null)
        {
            index[bucketCoords.x, bucketCoords.y, bucketCoords.z] = new List<int>();
        }

        return index[bucketCoords.x, bucketCoords.y, bucketCoords.z];
    }

    private Vector3Int getBucketCoords(Vector3 position)
    {
        position *= inverseRadius;
        return new Vector3Int
        (
            Mod(Mathf.FloorToInt(position.x), resX),
            Mod(Mathf.FloorToInt(position.y), resY),
            Mod(Mathf.FloorToInt(position.z), resZ)
        );
    }

    public void ForEachNeighbor(Vector3 point, Action<int> action)
    {
        Vector3Int coords = getBucketCoords(point);
        var diff = new[] { -1, 0, 1 };

        foreach (int dX in diff)
            foreach (int dY in diff)
                foreach (int dZ in diff)
        {
            int x = Mod(coords.x + dX, resX);
            int y = Mod(coords.y + dY, resY);
            int z = Mod(coords.z + dZ, resZ);

            foreach (int id in getBucket(new Vector3Int(x, y, z)))
            {
                action(id);
            }
        }
    }

    private int Mod(int a, int n) => (a % n + n) % n;

}
