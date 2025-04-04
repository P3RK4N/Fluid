using UnityEngine;

public class SdfCollider : MonoBehaviour
{
    void Awake()
    {
        ValidateCollider();
    }

    private void ValidateCollider()
    {
        //Count the number of valid colliders
        int colliderCount = 0;
        Collider foundCollider = null;

        if (TryGetComponent<BoxCollider>(out var box)) { colliderCount++; foundCollider = box; }
        if (TryGetComponent<SphereCollider>(out var sphere)) { colliderCount++; foundCollider = sphere; }
        if (TryGetComponent<MeshCollider>(out var mesh))
        {
            // Check if MeshCollider is using a skinned mesh or convex (which is deformable)
            if (mesh.sharedMesh.isReadable == false)
            {
                Debug.LogError("MeshCollider is not readable! Removing component.", this);
                Destroy(this);
                return;
            }
            colliderCount++; foundCollider = mesh;
        }

        // Ensure exactly one collider is present
        if (colliderCount != 1)
        {
            Debug.LogError("SdfCollider requires exactly ONE of: BoxCollider, SphereCollider, or MeshCollider! Removing component.", this);
            Destroy(this);
            return;
        }
    }
}
