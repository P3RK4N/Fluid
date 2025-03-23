using UnityEngine;

public class Fluid : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //void advect(U, dT, Qn) { } // Takes Velocity Field, Delta Time, Quantity q.
    /*
     * Bad approach - Euler:
     * Not taking current Q into account for dQ, but neighbors (jagged lines, but works well for low freq places)
     * Qn+1[i] = Qn[i] - dT * Un[i] * (Qn[i+1] - Qn[i-1]) / 2dX
     * 
     * Good approach - Locate previous position of particles with RK3 and use prev position to interpolate between Qn values for current position
     * Xp = Xg - dT * U(Xg) -> Euler approximation of previous position of Xg (but RK3 recommened)
     * 
     * Based on Xp, we interpolate linearly between neighbors of Qn[i] values to get Qn+1[i] (Recomened cubic polynomial interpolation -> clamp negatives to zero)
     * 
     */


    void external() { } // Adds external force to vector field.
    void project() { } // Ensures uncompressability
}
