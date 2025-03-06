using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

public class BucketIndex<T>
{
    private int resX;
    private int resY;
    private int resZ;
    private float radius;
    private float inverseRadius;

    Dictionary<int, int4> reverseIndex;
    List<int>[,,] index;

    public BucketIndex(float radius, int resX, int resY, int resZ = 1)
    {
        Assert.IsTrue(typeof(T) == typeof(Vector2) || typeof(T) == typeof(Vector3), "Type should be either Vector2 or Vector3");

        this.radius = radius;
        this.inverseRadius = 1.0f / radius;
        this.resX = resX;
        this.resY = resY;
        this.resZ = resZ;

        index = new List<int>[resX, resY, resZ];
        reverseIndex = new();
    }

    public void put(int id, T position)
    {
        Vector3Int bucketCoords = getBucketCoords(position);
        var bucket = getBucket(bucketCoords);
        var elementIndex = bucket.Count;
        
        bucket.Add(id);
        reverseIndex.Add(id, new int4(bucketCoords.x, bucketCoords.y, bucketCoords.z, elementIndex));
    }

    public void update(int id, T position)
    {
        remove(id);
        put(id, position);
    }

    public void remove(int id)
    {
        if (reverseIndex.TryGetValue(id, out int4 indices))
        {
            var bucket = index[indices.x, indices.y, indices.z];
            int elementIndex = indices.w;
            reverseIndex.Remove(id);

            // Swap and pop
            bucket[elementIndex] = bucket[bucket.Count - 1];
            bucket.RemoveAt(bucket.Count - 1);
        }
    }

    public void clear()
    {
        index = new List<int>[resX, resY, resZ];
        reverseIndex.Clear();
    }

    public List<int> getBucket(Vector3Int bucketCoords)
    {
        if (index[bucketCoords.x, bucketCoords.y, bucketCoords.z] == null)
        {
            index[bucketCoords.x, bucketCoords.y, bucketCoords.z] = new List<int>();
        }

        return index[bucketCoords.x, bucketCoords.y, bucketCoords.z];
    }

    private Vector3Int getBucketCoords(T position)
    {
        if (position is Vector3 pos3)
        {
            pos3 *= inverseRadius;
            return new Vector3Int
            (
                Mod(Mathf.FloorToInt(pos3.x), resX),
                Mod(Mathf.FloorToInt(pos3.y), resY),
                Mod(Mathf.FloorToInt(pos3.z), resZ)
            );
        }
        else if (position is Vector2 pos2)
        {
            pos2 *= inverseRadius;
            return new Vector3Int
            (
                Mod(Mathf.FloorToInt(pos2.x), resX),
                Mod(Mathf.FloorToInt(pos2.y), resY),
                0
            );
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    public void ForEachNeighbor(T point, Action<int> action)
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

    public static int Mod(int a, int n) => (a % n + n) % n;

}
