
public abstract class FluidCollider<T>
{
    public struct ColliderQueryResult
    {
        public T point;
        public T normal;
        public float distance2;
    }

    public abstract bool isPenetrating(T point, float radius, ColliderQueryResult result);
    public abstract ColliderQueryResult getClosestPoint(T point);
}
