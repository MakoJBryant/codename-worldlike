using UnityEditor;
using MakoJBryant.SolarSystem.Generation; // Required to access ShapeSettings

// Tells Unity that this custom editor is for the ShapeSettings ScriptableObject.
[CustomEditor(typeof(ShapeSettings))]
public class ShapeSettingsEditor : Editor
{
    // This simply draws the default inspector for the ShapeSettings ScriptableObject.
    // The real-time update logic is handled by PlanetGeneratorEditor.
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}