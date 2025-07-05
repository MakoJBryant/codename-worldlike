using UnityEngine;
using UnityEditor;

public class GuidValidator : EditorWindow
{
    [MenuItem("Tools/Validate Planet Asset GUIDs")]
    public static void ShowWindow()
    {
        GetWindow<GuidValidator>("GUID Validator");
    }

    string shapeGuid = "7ef9483b9e7dcea4e982bad718d7987b";   // Replace with your shape GUID
    string shadingGuid = "392671fe00b5e1148b2ebc4ae4fc0755"; // Replace with your shading GUID
    string heightMapComputeGuid = "3cc67f5536712e44f8226f8aa63c3741"; // Replace as needed
    string terrainMaterialGuid = "59b5601ea48361e41a859ffb2bd918bd"; // Replace as needed

    private void OnGUI()
    {
        GUILayout.Label("Planet Asset GUIDs to validate", EditorStyles.boldLabel);

        shapeGuid = EditorGUILayout.TextField("Shape GUID", shapeGuid);
        shadingGuid = EditorGUILayout.TextField("Shading GUID", shadingGuid);
        heightMapComputeGuid = EditorGUILayout.TextField("HeightMap Compute Shader GUID", heightMapComputeGuid);
        terrainMaterialGuid = EditorGUILayout.TextField("Terrain Material GUID", terrainMaterialGuid);

        if (GUILayout.Button("Validate GUIDs"))
        {
            ValidateGuid(shapeGuid, "Shape Settings");
            ValidateGuid(shadingGuid, "Shading Settings");
            ValidateGuid(heightMapComputeGuid, "Height Map Compute Shader");
            ValidateGuid(terrainMaterialGuid, "Terrain Material");
        }
    }

    void ValidateGuid(string guid, string description)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning($"[GUID Validator] {description} with GUID '{guid}' NOT found in project!");
        }
        else
        {
            Debug.Log($"[GUID Validator] {description} found at: {path}");
        }
    }
}
