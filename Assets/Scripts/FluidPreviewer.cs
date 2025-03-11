using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

public class FluidPreviewer : Previewer
{
    [SerializeField]
    public int numParticles;

    [SerializeField]
    public int indexResolution;

    public FluidSim2D sim;

    protected override Color draw(Vector2 coords)
    {
        throw new NotImplementedException();
    }

    private Vector3 coordToWorldPos(Vector2 coord)
    {
        Vector3 localPosition = new Vector3(0, coord.y, coord.x - 0.5f); // Y up, Z right

        // Apply object transform (scale, rotation, position)
        return transform.TransformPoint(localPosition);
    }
}

[CustomEditor(typeof(FluidPreviewer))]
[CanEditMultipleObjects]
public class FluidPreviewerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        FluidPreviewer t = target as FluidPreviewer;


        if (GUILayout.Button("Init"))
        {
            t.sim = new FluidSim2D(t.numParticles, t.indexResolution);
        }

        DrawDefaultInspector();
    }
}