#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SetMaterialToNone
{
    [MenuItem("Magic/Set Selected To Material Import None")]
    private static void SetSelectedToMaterialImportNone()
    {
        Object[] selectedObjects = Selection.objects;
        int changedCount = 0;

        foreach (Object selectedObject in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(selectedObject);
            if (string.IsNullOrEmpty(path))
                continue;

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
                continue;

            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();

            changedCount++;
            Debug.Log("Set material import to None: " + path);
        }

        Debug.Log("Done. Updated " + changedCount + " model asset(s).");
    }

    [MenuItem("Tools/Models/Set Selected To Material Import None", true)]
    private static bool ValidateSetSelectedToMaterialImportNone()
    {
        foreach (Object selectedObject in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(selectedObject);
            if (string.IsNullOrEmpty(path))
                continue;

            if (AssetImporter.GetAtPath(path) is ModelImporter)
                return true;
        }

        return false;
    }
}
#endif