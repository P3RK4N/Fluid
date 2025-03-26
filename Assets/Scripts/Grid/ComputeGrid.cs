using System.Text;
using UnityEngine;

public class ComputeGrid : MonoBehaviour
{
    [EditorOnly] public ComputeShader computeGrid;
    [EditorOnly] public int bucketCapacity = 10;
    [EditorOnly] public int gridSize = 10;

    ComputeBuffer gridBuffer;

    int numPoints;

    void Awake()
    {
        gridBuffer = new ComputeBuffer(gridSize * gridSize * gridSize * bucketCapacity, sizeof(uint));
        computeGrid.SetBuffer(0, "IndexGrid", gridBuffer);
        computeGrid.SetBuffer(1, "IndexGrid", gridBuffer);
    }

    public void InitializeGrid(ComputeBuffer points, ComputeShader clientShader, params int[] kernels)
    {
        Debug.Assert(points.count <= gridBuffer.count * 0.75f, "Grid buffer too small, make it bigger!");

        computeGrid.SetInt("_gridSize", gridSize);
        computeGrid.SetInt("_gridSize2", gridSize * gridSize);
        computeGrid.SetInt("_gridSize3", gridSize * gridSize * gridSize);
        computeGrid.SetInt("_bucketCapacity", bucketCapacity);
        computeGrid.SetInt("numPoints", points.count);
        computeGrid.SetBuffer(0, "points", points);
        computeGrid.SetBuffer(1, "points", points);
        numPoints = points.count;

        clientShader.SetInt("_gridSize", gridSize);
        clientShader.SetInt("_gridSize2", gridSize * gridSize);
        clientShader.SetInt("_gridSize3", gridSize * gridSize * gridSize);
        clientShader.SetInt("_bucketCapacity", bucketCapacity);
        foreach (var kernel in kernels)
        {
            clientShader.SetBuffer(kernel, "IndexGrid", gridBuffer);
        }
    }

    public void RecalculateGrid(float bucketRadius, ComputeShader clientShader)
    {
        clientShader.SetFloat("_bucketRadius", bucketRadius);
        clientShader.SetFloat("_inverseBucketRadius", 1.0f / bucketRadius);
        computeGrid.SetFloat("_bucketRadius", bucketRadius);
        computeGrid.SetFloat("_inverseBucketRadius", 1.0f / bucketRadius);

        computeGrid.Dispatch(0 /* Clear */, Mathf.CeilToInt((float)(gridSize * gridSize * gridSize * bucketCapacity) / 1024), 1, 1);
        //printBuckets(0);
        computeGrid.Dispatch(1 /* Init  */, Mathf.CeilToInt((float)(numPoints) / 1024), 1, 1);
        printBuckets(1);
    }

    private void OnDestroy()
    {
        gridBuffer.Release();
    }

#region Debug

    void printBuckets(int id)
    {
        uint[] data = new uint[gridSize * gridSize * gridSize * bucketCapacity];
        gridBuffer.GetData(data);

        // Define the dimensions
        int bucketSize = bucketCapacity - 1; // Size of each bucket excluding the count

        // Iterate through the data array
        int totalBuckets = gridSize * gridSize * gridSize; // Total number of buckets in the grid

        for (int i = 0; i < totalBuckets; i++)
        {
            // Calculate the index of the last element in the current bucket (count element)
            int lastElementIndex = (i + 1) * bucketCapacity - 1;
            int firstElementIndex = i * bucketCapacity;

            // Get the count from the last element of the bucket
            uint count = data[lastElementIndex];
            int real_count = Mathf.Min((int)count, bucketSize);

            // If the bucket is non-empty, print the count and its corresponding bucket index
            if (count > 0)
            {
                // Calculate the 4D index from the flattened index
                int z = i / (gridSize * gridSize);
                int y = (i / gridSize) % gridSize;
                int x = i % gridSize;

                StringBuilder sb = new StringBuilder();
                for (int j = firstElementIndex; j < firstElementIndex + real_count; j++)
                {
                    sb.Append(data[j] + " ");
                }

                // Print the bucket index and its count
                // Print bucket contents
                Debug.Log($"{id}> Bucket [{x}, {y}, {z}] has count: {count}\n{sb.ToString()}");
            }
        }
    }

#endregion Debug

}
