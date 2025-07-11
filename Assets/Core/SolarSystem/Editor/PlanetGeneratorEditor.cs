using UnityEngine;
using UnityEditor; // Required for Editor class
using MakoJBryant.SolarSystem.Generation; // Required to access PlanetGenerator

// Tells Unity that this custom editor is for the PlanetGenerator script.
[CustomEditor(typeof(PlanetGenerator))]
public class PlanetGeneratorEditor : Editor
{
    PlanetGenerator planetGenerator; // Reference to the PlanetGenerator instance being inspected
    Editor shapeSettingsEditor;     // Editor for ShapeSettings ScriptableObject
    Editor colorSettingsEditor;     // Editor for ColorSettings ScriptableObject

    // This method is called when the Inspector is opened or when the selection changes.
    // It's used to initialize references.
    void OnEnable()
    {
        planetGenerator = (PlanetGenerator)target; // 'target' is the object this editor is inspecting
    }

    // This is the main method where you draw your custom Inspector UI.
    public override void OnInspectorGUI()
    {
        // Draw the default Inspector for the PlanetGenerator script first.
        // This will show 'Resolution', 'Radius', 'Shape Settings', and 'Color Settings'.
        DrawDefaultInspector();

        // Check if settings assets are assigned.
        // If not, display a warning and don't try to draw their sub-editors.
        if (planetGenerator.shapeSettings == null)
        {
            EditorGUILayout.HelpBox("Shape Settings asset is not assigned!", MessageType.Warning);
        }
        if (planetGenerator.colorSettings == null)
        {
            EditorGUILayout.HelpBox("Color Settings asset is not assigned!", MessageType.Warning);
        }

        // Add a button to manually generate the planet (useful for debugging or specific control)
        if (GUILayout.Button("Generate Planet Now"))
        {
            planetGenerator.GeneratePlanet();
        }

        // --- Draw Nested Editors for ScriptableObjects ---
        // This is where the magic happens for real-time updates and organized UI.

        // Draw Shape Settings Editor
        if (planetGenerator.shapeSettings != null)
        {
            // Create a foldout for Shape Settings
            // 'Editor.CreateEditor' creates an editor for the ScriptableObject.
            // 'EditorGUILayout.InspectorTitlebar' provides a collapsible header.
            using (var check = new EditorGUI.ChangeCheckScope()) // Detects changes to properties drawn within this scope
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Shape Settings", EditorStyles.boldLabel);
                CreateCachedEditor(planetGenerator.shapeSettings, typeof(ShapeSettingsEditor), ref shapeSettingsEditor);
                shapeSettingsEditor.OnInspectorGUI();

                if (check.changed) // If any property in ShapeSettings changed
                {
                    // Mark the asset as dirty so changes are saved
                    EditorUtility.SetDirty(planetGenerator.shapeSettings);
                    // Automatically regenerate the planet
                    planetGenerator.GeneratePlanet();
                }
            }
        }

        // Draw Color Settings Editor
        if (planetGenerator.colorSettings != null)
        {
            using (var check = new EditorGUI.ChangeCheckScope()) // Detects changes to properties drawn within this scope
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Color Settings", EditorStyles.boldLabel);
                CreateCachedEditor(planetGenerator.colorSettings, typeof(ColorSettingsEditor), ref colorSettingsEditor);
                colorSettingsEditor.OnInspectorGUI();

                if (check.changed) // If any property in ColorSettings changed
                {
                    // Mark the asset as dirty so changes are saved
                    EditorUtility.SetDirty(planetGenerator.colorSettings);
                    // Automatically regenerate the planet
                    planetGenerator.GeneratePlanet();
                }
            }
        }
    }
}