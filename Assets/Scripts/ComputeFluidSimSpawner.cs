using System;
using UnityEngine;
using static ComputeFluidSim;

public class ComputeFluidSimSpawner : MonoBehaviour
{
    [EditorOnly] public float size = 1.0f;
    [EditorOnly] public Vector3 offset = Vector3.zero;

    ComputeFluidSim sim;
    Transform childBounds;

    private void Awake()
    {
        sim = GetComponent<ComputeFluidSim>();
        childBounds = transform.GetChild(0);
    }

    private void Start()
    {
        spawn(childBounds, sim.positionBuffer, sim.dimension);
    }

    private void spawn(Transform tf, ComputeBuffer positionBuffer, Dimension dimension)
    {
        int count = positionBuffer.count;

        Vector3 width = new Vector3(tf.lossyScale.x, tf.lossyScale.y, tf.lossyScale.z);
        Vector3 halfWidth = width / 2.0f;
        var mat = Matrix4x4.TRS(tf.position, tf.rotation, Vector3.one);

        // For 2D
        if (dimension == Dimension.Dimension2D)
        {
            Vector2[] initialPositions = new Vector2[count];
            int rowSize = Mathf.CeilToInt(Mathf.Sqrt((float)count));
            Vector3 spacing = width / rowSize;
            for (int i = 0; i < count; i++)
            {
                int x = i / rowSize;
                int y = i % rowSize;
                initialPositions[i] = mat * new Vector4(spacing.x * x - halfWidth.x, spacing.x * y - halfWidth.x, 0, 1);
            }
            positionBuffer.SetData(initialPositions);
        }

        // For 3D
        else
        {
            Vector3[] initialPositions = new Vector3[count];
            int rowSize = Mathf.CeilToInt(Mathf.Pow((float)count, 1.0f / 3.0f));
            int sliceSize = rowSize * rowSize;
            Vector3 spacing = width / rowSize;
            for (int i = 0; i < count; i++)
            {
                int x = i / sliceSize;
                int y = (i % sliceSize) / rowSize;
                int z = i % rowSize;
                initialPositions[i] = mat * new Vector4(spacing.x * x - halfWidth.x, spacing.y * y - halfWidth.y, spacing.z * z - halfWidth.z, 1.0f);
            }
            positionBuffer.SetData(initialPositions);
        }

    }
}
