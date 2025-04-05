using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GridGenerator : MonoBehaviour
{
    [EditorOnly] public Vector2Int resolution = new Vector2Int(5, 5);
    public float cellSize = 1.2f; // Space between planes
    private TextMeshPro[,] textCells;

    void Awake()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        textCells = new TextMeshPro[resolution.x, resolution.y];

        for (int row = 0; row < resolution.x; row++)
        {
            for (int col = 0; col < resolution.y; col++)
            {
                Vector3 position = new Vector3(col * cellSize, 0, -row * cellSize);
                GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.transform.position = position;
                plane.transform.localScale = new Vector3(0.1f, 1, 0.1f); // Adjust plane size
                plane.transform.SetParent(transform);

                // Create a text object
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(plane.transform, false);
                textObj.transform.localPosition = new Vector3(0, 0.01f, 0); // Slightly above the plane
                textObj.transform.rotation = Quaternion.Euler(90, 0, 0);

                TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
                textMesh.text = "0";
                textMesh.fontSize = 30;
                textMesh.alignment = TextAlignmentOptions.Center;
                textMesh.rectTransform.sizeDelta = new Vector2(1, 1);
                textMesh.textWrappingMode = TextWrappingModes.NoWrap;

                textCells[row, col] = textMesh;

                // Create a text object
                GameObject textTitleObj = new GameObject("Text");
                textTitleObj.transform.SetParent(plane.transform, false);
                textTitleObj.transform.localPosition = new Vector3(0, 0.01f, 5.0f); // Slightly above the plane
                textTitleObj.transform.rotation = Quaternion.Euler(90, 0, 0);

                TextMeshPro textTitleMesh = textTitleObj.AddComponent<TextMeshPro>();
                textTitleMesh.text = row + "," + col;
                textTitleMesh.fontSize = 30;
                textTitleMesh.alignment = TextAlignmentOptions.Top;
                textTitleMesh.rectTransform.sizeDelta = new Vector2(1, 1);
                textTitleMesh.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }
    }

    public string this[int row, int col]
    {
        get { return textCells[row, col]?.text; }
        set { if (textCells[row, col] != null) textCells[row, col].text = value; }
    }
}