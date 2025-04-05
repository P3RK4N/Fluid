using UnityEngine;
using TMPro;

public class GridHasher : MonoBehaviour
{
    public int x = 0, y = 0;
    public int totalSteps;
    
    GridGenerator gridGenerator;

    void Awake()
    {
        gridGenerator = GetComponent<GridGenerator>();
    }

    void Start()
    {
        for (int s = 0; s < totalSteps; s++)
        {
            (int i, int j) = HashFunction(x, y, s);
            IncrementCell(i, j);
        }
    }

    (int, int) HashFunction(int x, int y, int step)
    {
        int newX = (x ^ (step * 73856093)) % gridGenerator.resolution.x;
        int newY = (y ^ (step * 19349663)) % gridGenerator.resolution.y;

        return (Mathf.Abs(newX), Mathf.Abs(newY));
    }

    void IncrementCell(int row, int col)
    {
        if (row < 0 || row >= gridGenerator.resolution.x || col < 0 || col >= gridGenerator.resolution.y)
            return;

        int currentValue = int.TryParse(gridGenerator[row, col], out int result) ? result : 0;
        gridGenerator[row, col] = (currentValue + 1).ToString();
    }
}
