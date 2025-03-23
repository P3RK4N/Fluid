using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(EditorOnlyAttribute))]
public class EditorOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = !Application.isPlaying; // Disable in Play Mode
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}