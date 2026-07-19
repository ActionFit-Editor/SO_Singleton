using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ActionFit.SOSingleton;
using UnityEditor;
using UnityEngine;

namespace ActionFit.SOSingleton.Editor
{
    public enum ActionFitSettingsAssetStatus
    {
        FoundCanonical,
        FoundLegacy,
        FoundUnique,
        Created,
        Missing,
        Duplicate,
        InvalidRegistration,
        InvalidRuntimeBase,
        OccupiedCanonicalPath,
        CreationBlocked,
        CreationFailed
    }

    public sealed class ActionFitSettingsAssetResolution
    {
        internal ActionFitSettingsAssetResolution(
            Type assetType,
            ActionFitSettingsAssetStatus status,
            string canonicalPath,
            string actualPath,
            ScriptableObject asset,
            string diagnostic,
            bool isCacheHit = false)
        {
            AssetType = assetType;
            Status = status;
            CanonicalPath = canonicalPath ?? string.Empty;
            ActualPath = actualPath ?? string.Empty;
            Asset = asset;
            Diagnostic = diagnostic ?? string.Empty;
            IsCacheHit = isCacheHit;
        }

        public Type AssetType { get; }
        public ActionFitSettingsAssetStatus Status { get; }
        public string CanonicalPath { get; }
        public string ActualPath { get; }
        public ScriptableObject Asset { get; }
        public string Diagnostic { get; }
        public bool IsCacheHit { get; }
        public bool IsSuccess => Status is ActionFitSettingsAssetStatus.FoundCanonical or
            ActionFitSettingsAssetStatus.FoundLegacy or
            ActionFitSettingsAssetStatus.FoundUnique or
            ActionFitSettingsAssetStatus.Created;

        internal ActionFitSettingsAssetResolution AsCacheHit()
        {
            return new ActionFitSettingsAssetResolution(
                AssetType,
                Status,
                CanonicalPath,
                ActualPath,
                Asset,
                Diagnostic,
                true);
        }
    }

    [InitializeOnLoad]
    public static class ActionFitSettingsAssetProvider
    {
        private static readonly Dictionary<Type, ActionFitSettingsAssetResolution> Cache = new();
        private static readonly Dictionary<Type, string[]> ProjectAssetPathsByType = new();
        private static IReadOnlyList<Type> _registeredTypes;
        private static string[] _projectScriptableObjectPaths;

        internal static Func<bool> CreationAllowedOverride { get; set; }

        static ActionFitSettingsAssetProvider()
        {
            EditorApplication.delayCall += EnsureRegisteredAssets;
            EditorApplication.projectChanged += ClearCache;
            EditorApplication.playModeStateChanged += _ => ClearCache();
            AssemblyReloadEvents.beforeAssemblyReload += ClearCache;
        }

        /// <summary>등록된 설정 타입을 탐색하고 필요할 때 표준 경로에 생성합니다.</summary>
        public static ActionFitSettingsAssetResolution Resolve(Type assetType, bool createIfMissing = true)
        {
            if (assetType == null)
            {
                return Result(
                    null,
                    ActionFitSettingsAssetStatus.InvalidRegistration,
                    diagnostic: "Asset type is null.");
            }

            ActionFitSettingsAssetAttribute registration =
                assetType.GetCustomAttribute<ActionFitSettingsAssetAttribute>(false);
            return Resolve(assetType, registration, createIfMissing);
        }

        /// <summary>등록된 설정 타입을 탐색하고 강타입 에셋을 반환합니다.</summary>
        public static T GetOrCreate<T>() where T : ScriptableObject
        {
            return Resolve(typeof(T), true).Asset as T;
        }

        /// <summary>등록된 모든 설정 타입을 변경 없이 검사합니다.</summary>
        public static IReadOnlyList<ActionFitSettingsAssetResolution> AuditAll()
        {
            return RegisteredTypes
                .Select(Audit)
                .ToArray();
        }

        /// <summary>등록된 설정 타입을 확인하고 누락된 auto-create 에셋을 표준 경로에 생성합니다.</summary>
        public static void EnsureRegisteredAssets()
        {
            if (!CanCreateNow())
            {
                return;
            }

            foreach (Type type in RegisteredTypes)
            {
                if (HasUniqueProjectAssetWithoutLoading(type))
                {
                    continue;
                }

                ActionFitSettingsAssetResolution result = Resolve(type, true);
                if (!result.IsSuccess && result.Status != ActionFitSettingsAssetStatus.Missing)
                {
                    Debug.LogError(
                        $"[ActionFitSettingsAssetProvider] Bootstrap: {type.FullName} " +
                        $"{result.Status} - {result.Diagnostic}");
                }
            }
        }

        /// <summary>타입별 탐색 결과 캐시를 비웁니다.</summary>
        public static void ClearCache()
        {
            Cache.Clear();
            ProjectAssetPathsByType.Clear();
            _projectScriptableObjectPaths = null;
        }

        internal static ActionFitSettingsAssetResolution ResolveForTests(
            Type assetType,
            ActionFitSettingsAssetAttribute registration,
            bool createIfMissing)
        {
            Cache.Remove(assetType);
            return Resolve(assetType, registration, createIfMissing);
        }

        internal static ActionFitSettingsAssetResolution AuditForTests(
            Type assetType,
            ActionFitSettingsAssetAttribute registration)
        {
            return Audit(assetType, registration);
        }

        internal static void ResetForTests()
        {
            Cache.Clear();
            ProjectAssetPathsByType.Clear();
            _registeredTypes = null;
            _projectScriptableObjectPaths = null;
            CreationAllowedOverride = null;
        }

        private static IReadOnlyList<Type> RegisteredTypes =>
            _registeredTypes ??= TypeCache
                .GetTypesWithAttribute<ActionFitSettingsAssetAttribute>()
                .Where(type => !type.IsAbstract)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();

        private static ActionFitSettingsAssetResolution Audit(Type assetType)
        {
            ActionFitSettingsAssetAttribute registration =
                assetType?.GetCustomAttribute<ActionFitSettingsAssetAttribute>(false);
            return Audit(assetType, registration);
        }

        private static ActionFitSettingsAssetResolution Audit(
            Type assetType,
            ActionFitSettingsAssetAttribute registration)
        {
            if (assetType == null ||
                !typeof(ScriptableObject).IsAssignableFrom(assetType) ||
                assetType.IsAbstract)
            {
                return Result(
                    assetType,
                    ActionFitSettingsAssetStatus.InvalidRegistration,
                    diagnostic: "Registered type must be a concrete ScriptableObject.");
            }

            if (registration == null)
            {
                return Result(
                    assetType,
                    ActionFitSettingsAssetStatus.InvalidRegistration,
                    diagnostic: "ActionFitSettingsAssetAttribute is missing.");
            }

            if (!TryGetCanonicalPath(assetType, registration, out string canonicalPath, out string diagnostic))
            {
                return Result(
                    assetType,
                    ActionFitSettingsAssetStatus.InvalidRegistration,
                    diagnostic: diagnostic);
            }

            if (registration.Lifetime == ActionFitSettingsAssetLifetime.RuntimeSingleton &&
                !HasMatchingSingletonBase(assetType))
            {
                return Result(
                    assetType,
                    ActionFitSettingsAssetStatus.InvalidRuntimeBase,
                    canonicalPath,
                    diagnostic: $"{assetType.FullName} must inherit SO_Singleton<{assetType.Name}>.");
            }

            Type canonicalAssetType = AssetDatabase.GetMainAssetTypeAtPath(canonicalPath);
            if (canonicalAssetType != null && !assetType.IsAssignableFrom(canonicalAssetType))
            {
                return Result(
                    assetType,
                    ActionFitSettingsAssetStatus.OccupiedCanonicalPath,
                    canonicalPath,
                    canonicalPath,
                    diagnostic: "Canonical path is occupied by another asset type.");
            }

            string[] projectMatches = FindProjectAssets(assetType);
            if (projectMatches.Length > 1)
            {
                return DuplicateResult(assetType, canonicalPath, projectMatches);
            }

            if (canonicalAssetType != null)
            {
                return Result(
                    assetType,
                    ActionFitSettingsAssetStatus.FoundCanonical,
                    canonicalPath,
                    canonicalPath);
            }

            string[] legacyMatches = FindLegacyAssets(assetType, registration);
            if (legacyMatches.Length > 1)
            {
                return DuplicateResult(assetType, canonicalPath, legacyMatches);
            }

            if (legacyMatches.Length == 1)
            {
                return Result(
                    assetType,
                    ActionFitSettingsAssetStatus.FoundLegacy,
                    canonicalPath,
                    legacyMatches[0]);
            }

            if (projectMatches.Length == 1)
            {
                return Result(
                    assetType,
                    ActionFitSettingsAssetStatus.FoundUnique,
                    canonicalPath,
                    projectMatches[0]);
            }

            return Result(
                assetType,
                ActionFitSettingsAssetStatus.Missing,
                canonicalPath,
                diagnostic: "No project settings asset was found.");
        }

        private static ActionFitSettingsAssetResolution Resolve(
            Type assetType,
            ActionFitSettingsAssetAttribute registration,
            bool createIfMissing)
        {
            if (Cache.TryGetValue(assetType, out ActionFitSettingsAssetResolution cached))
            {
                if (IsSuccessfulStatus(cached.Status) && cached.Asset == null)
                {
                    Cache.Remove(assetType);
                }
                else if (!(createIfMissing && cached.Status == ActionFitSettingsAssetStatus.Missing))
                {
                    return cached.AsCacheHit();
                }
            }

            if (!typeof(ScriptableObject).IsAssignableFrom(assetType) || assetType.IsAbstract)
            {
                return CacheResult(Result(
                    assetType,
                    ActionFitSettingsAssetStatus.InvalidRegistration,
                    diagnostic: "Registered type must be a concrete ScriptableObject."));
            }

            if (registration == null)
            {
                return CacheResult(Result(
                    assetType,
                    ActionFitSettingsAssetStatus.InvalidRegistration,
                    diagnostic: "ActionFitSettingsAssetAttribute is missing."));
            }

            if (!TryGetCanonicalPath(assetType, registration, out string canonicalPath, out string diagnostic))
            {
                return CacheResult(Result(
                    assetType,
                    ActionFitSettingsAssetStatus.InvalidRegistration,
                    diagnostic: diagnostic));
            }

            if (registration.Lifetime == ActionFitSettingsAssetLifetime.RuntimeSingleton &&
                !HasMatchingSingletonBase(assetType))
            {
                return CacheResult(Result(
                    assetType,
                    ActionFitSettingsAssetStatus.InvalidRuntimeBase,
                    canonicalPath,
                    diagnostic: $"{assetType.FullName} must inherit SO_Singleton<{assetType.Name}>."));
            }

            UnityEngine.Object canonicalObject = AssetDatabase.LoadMainAssetAtPath(canonicalPath);
            if (canonicalObject != null)
            {
                if (canonicalObject is ScriptableObject canonicalAsset && assetType.IsInstanceOfType(canonicalAsset))
                {
                    string[] canonicalMatches = FindProjectAssets(assetType);
                    if (canonicalMatches.Length > 1)
                    {
                        return CacheResult(DuplicateResult(assetType, canonicalPath, canonicalMatches));
                    }

                    return CacheResult(Result(
                        assetType,
                        ActionFitSettingsAssetStatus.FoundCanonical,
                        canonicalPath,
                        canonicalPath,
                        canonicalAsset));
                }

                return CacheResult(Result(
                    assetType,
                    ActionFitSettingsAssetStatus.OccupiedCanonicalPath,
                    canonicalPath,
                    canonicalPath,
                    diagnostic: "Canonical path is occupied by another asset type."));
            }

            string[] projectMatches = FindProjectAssets(assetType);
            if (projectMatches.Length > 1)
            {
                return CacheResult(DuplicateResult(assetType, canonicalPath, projectMatches));
            }

            string[] legacyMatches = FindLegacyAssets(assetType, registration);

            if (legacyMatches.Length > 1)
            {
                return CacheResult(DuplicateResult(assetType, canonicalPath, legacyMatches));
            }

            if (legacyMatches.Length == 1)
            {
                return CacheResult(Result(
                    assetType,
                    ActionFitSettingsAssetStatus.FoundLegacy,
                    canonicalPath,
                    legacyMatches[0],
                    AssetDatabase.LoadAssetAtPath(legacyMatches[0], assetType) as ScriptableObject));
            }

            if (projectMatches.Length == 1)
            {
                return CacheResult(Result(
                    assetType,
                    ActionFitSettingsAssetStatus.FoundUnique,
                    canonicalPath,
                    projectMatches[0],
                    AssetDatabase.LoadAssetAtPath(projectMatches[0], assetType) as ScriptableObject));
            }

            if (!createIfMissing || !registration.AutoCreate)
            {
                return CacheResult(Result(
                    assetType,
                    ActionFitSettingsAssetStatus.Missing,
                    canonicalPath,
                    diagnostic: "No project settings asset was found."));
            }

            if (!CanCreateNow())
            {
                return CacheResult(Result(
                    assetType,
                    ActionFitSettingsAssetStatus.CreationBlocked,
                    canonicalPath,
                    diagnostic: "Asset creation is disabled during Play Mode transitions."));
            }

            return CacheResult(CreateAsset(assetType, canonicalPath));
        }

        private static ActionFitSettingsAssetResolution CreateAsset(Type assetType, string canonicalPath)
        {
            ScriptableObject instance = null;
            bool created = false;

            try
            {
                EnsureFolderExists(canonicalPath.Substring(0, canonicalPath.LastIndexOf('/')));
                instance = ScriptableObject.CreateInstance(assetType);
                if (instance is IActionFitSettingsAssetInitializer initializer)
                {
                    initializer.InitializeNewSettingsAsset();
                }

                AssetDatabase.CreateAsset(instance, canonicalPath);
                created = true;

                ScriptableObject loaded = AssetDatabase.LoadAssetAtPath(canonicalPath, assetType) as ScriptableObject;
                if (loaded == null)
                {
                    throw new InvalidOperationException("Created asset could not be loaded from its canonical path.");
                }

                return Result(
                    assetType,
                    ActionFitSettingsAssetStatus.Created,
                    canonicalPath,
                    canonicalPath,
                    loaded);
            }
            catch (Exception exception)
            {
                if (created && AssetDatabase.LoadMainAssetAtPath(canonicalPath) == instance)
                {
                    AssetDatabase.DeleteAsset(canonicalPath);
                }
                else if (instance != null && !AssetDatabase.Contains(instance))
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }

                return Result(
                    assetType,
                    ActionFitSettingsAssetStatus.CreationFailed,
                    canonicalPath,
                    diagnostic: exception.Message);
            }
        }

        private static bool CanCreateNow()
        {
            if (CreationAllowedOverride != null)
            {
                return CreationAllowedOverride();
            }

            return !EditorApplication.isPlaying &&
                   !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static bool TryGetCanonicalPath(
            Type assetType,
            ActionFitSettingsAssetAttribute registration,
            out string canonicalPath,
            out string diagnostic)
        {
            diagnostic = string.Empty;

            if (!string.IsNullOrWhiteSpace(registration.CanonicalPath))
            {
                canonicalPath = NormalizePath(registration.CanonicalPath);
                if (!IsCanonicalProjectPath(canonicalPath))
                {
                    diagnostic = "CanonicalPath must be an .asset path under Assets/_Data/.";
                    return false;
                }

                return true;
            }

            string owner = registration.Owner?.Trim();
            if (string.IsNullOrEmpty(owner) ||
                owner != registration.Owner ||
                owner is "." or ".." ||
                owner.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
            {
                canonicalPath = string.Empty;
                diagnostic = "Owner must be one safe _Data folder segment.";
                return false;
            }

            string folder = $"Assets/_Data/_{owner}";
            canonicalPath = registration.Lifetime == ActionFitSettingsAssetLifetime.RuntimeSingleton
                ? $"{folder}/Resources/SO/{assetType.Name}.asset"
                : $"{folder}/{assetType.Name}.asset";
            return true;
        }

        private static bool HasMatchingSingletonBase(Type assetType)
        {
            for (Type current = assetType.BaseType; current != null; current = current.BaseType)
            {
                if (!current.IsGenericType ||
                    current.GetGenericTypeDefinition() != typeof(SO_Singleton<>))
                {
                    continue;
                }

                Type[] arguments = current.GetGenericArguments();
                return arguments.Length == 1 && arguments[0] == assetType;
            }

            return false;
        }

        private static string[] FindProjectAssets(Type assetType)
        {
            if (ProjectAssetPathsByType.TryGetValue(assetType, out string[] cachedPaths))
            {
                return cachedPaths;
            }

            _projectScriptableObjectPaths ??= AssetDatabase
                .FindAssets("t:ScriptableObject", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            string[] paths = _projectScriptableObjectPaths
                .Where(path =>
                {
                    Type mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                    return mainAssetType != null && assetType.IsAssignableFrom(mainAssetType);
                })
                .ToArray();
            ProjectAssetPathsByType[assetType] = paths;
            return paths;
        }

        private static string[] FindLegacyAssets(
            Type assetType,
            ActionFitSettingsAssetAttribute registration)
        {
            return (registration.LegacyPaths ?? Array.Empty<string>())
                .Where(IsProjectAssetPath)
                .Select(NormalizePath)
                .Distinct(StringComparer.Ordinal)
                .Where(path =>
                {
                    Type mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                    return mainAssetType != null && assetType.IsAssignableFrom(mainAssetType);
                })
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool HasUniqueProjectAssetWithoutLoading(Type assetType)
        {
            ActionFitSettingsAssetAttribute registration =
                assetType.GetCustomAttribute<ActionFitSettingsAssetAttribute>(false);
            if (registration == null ||
                !typeof(ScriptableObject).IsAssignableFrom(assetType) ||
                assetType.IsAbstract ||
                !TryGetCanonicalPath(assetType, registration, out string canonicalPath, out _) ||
                registration.Lifetime == ActionFitSettingsAssetLifetime.RuntimeSingleton &&
                !HasMatchingSingletonBase(assetType))
            {
                return false;
            }

            Type canonicalAssetType = AssetDatabase.GetMainAssetTypeAtPath(canonicalPath);
            if (canonicalAssetType != null && !assetType.IsAssignableFrom(canonicalAssetType))
            {
                return false;
            }

            return FindProjectAssets(assetType).Length == 1;
        }

        private static ActionFitSettingsAssetResolution DuplicateResult(
            Type assetType,
            string canonicalPath,
            IReadOnlyCollection<string> matches)
        {
            return Result(
                assetType,
                ActionFitSettingsAssetStatus.Duplicate,
                canonicalPath,
                diagnostic: $"Multiple project assets were found: {string.Join(", ", matches)}");
        }

        private static ActionFitSettingsAssetResolution Result(
            Type assetType,
            ActionFitSettingsAssetStatus status,
            string canonicalPath = "",
            string actualPath = "",
            ScriptableObject asset = null,
            string diagnostic = "")
        {
            return new ActionFitSettingsAssetResolution(
                assetType,
                status,
                canonicalPath,
                actualPath,
                asset,
                diagnostic);
        }

        private static ActionFitSettingsAssetResolution CacheResult(
            ActionFitSettingsAssetResolution result)
        {
            if (result.AssetType != null)
            {
                Cache[result.AssetType] = result;
            }

            return result;
        }

        private static bool IsSuccessfulStatus(ActionFitSettingsAssetStatus status)
        {
            return status is ActionFitSettingsAssetStatus.FoundCanonical or
                ActionFitSettingsAssetStatus.FoundLegacy or
                ActionFitSettingsAssetStatus.FoundUnique or
                ActionFitSettingsAssetStatus.Created;
        }

        private static bool IsProjectAssetPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   NormalizePath(path).StartsWith("Assets/", StringComparison.Ordinal);
        }

        private static bool IsCanonicalProjectPath(string path)
        {
            return path.StartsWith("Assets/_Data/", StringComparison.Ordinal) &&
                   path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) &&
                   path.IndexOf("../", StringComparison.Ordinal) < 0 &&
                   path.IndexOf('\\') < 0;
        }

        private static string NormalizePath(string path)
        {
            return path?.Trim().Replace('\\', '/');
        }

        private static void EnsureFolderExists(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];

            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }
}
