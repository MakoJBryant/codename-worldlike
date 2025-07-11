using UnityEditor;
using MakoJBryant.SolarSystem.Generation; // Required to access ColorSettings

// Tells Unity that this custom editor is for the ColorSettings ScriptableObject.
[CustomEditor(typeof(ColorSettings))]
public class ColorSettingsEditor : Editor
{
    // This simply draws the default inspector for the ColorSettings ScriptableObject.
    // The real-time update logic is handled by PlanetGeneratorEditor.
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}