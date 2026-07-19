#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ActionFit.SOSingleton.Editor;

public static class SoSingletonPackageMenu
{
    private const string MenuRoot = "Tools/Package/SO Singleton/";
    private const string ReadmePath = "Packages/com.actionfit.sosingleton/README.md";
    private const int AuditPriority = 100;
    private const int ReadmePriority = 901;

    [MenuItem(MenuRoot + "Audit Settings SO", false, AuditPriority)]
    public static void AuditSettingsSo()
    {
        var results = ActionFitSettingsAssetProvider.AuditAll();
        int failures = 0;
        foreach (var result in results)
        {
            string typeName = result.AssetType?.FullName ?? "(null)";
            string actualPath = string.IsNullOrEmpty(result.ActualPath) ? "(none)" : result.ActualPath;
            string message =
                $"[SO Singleton] Audit: {typeName} | {result.Status} | " +
                $"actual={actualPath} | canonical={result.CanonicalPath} | " +
                $"cache={result.IsCacheHit} | diagnostic={result.Diagnostic}";
            if (result.IsSuccess)
            {
                Debug.Log(message);
            }
            else
            {
                failures++;
                Debug.LogError(message);
            }
        }

        Debug.Log(
            $"[SO Singleton] Audit: completed {results.Count} registered settings type(s), " +
            $"failures={failures}.");
    }

    [MenuItem(MenuRoot + "Clear Settings Cache", false, AuditPriority + 1)]
    private static void ClearSettingsCache()
    {
        ActionFitSettingsAssetProvider.ClearCache();
        Debug.Log("[SO Singleton] Clear Settings Cache: completed.");
    }

    [MenuItem(MenuRoot + "README", false, ReadmePriority)]
    private static void OpenReadme()
    {
        var readme = AssetDatabase.LoadAssetAtPath<TextAsset>(ReadmePath);
        if (readme == null)
        {
            EditorUtility.DisplayDialog("Package README", $"README was not found.\n{ReadmePath}", "OK");
            return;
        }

        Selection.activeObject = readme;
        AssetDatabase.OpenAsset(readme);
    }
}
#endif
