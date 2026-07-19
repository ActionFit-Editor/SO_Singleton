using ActionFit.SOSingleton.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ActionFit.SOSingleton.Editor.Tests
{
    public sealed class SOSettingsLifecycleTests
    {
        private const string TestRoot = "Assets/_Data/_MCC1541Tests";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            ActionFitSettingsAssetProvider.ResetForTests();
            ActionFitSettingsAssetProvider.CreationAllowedOverride = () => true;
        }

        [TearDown]
        public void TearDown()
        {
            ActionFitSettingsAssetProvider.ResetForTests();
            SO_SingletonRuntimeReset.ResetAllForTests();
            SOSettingsRuntimeFixture.RestoreLoaderForTests();
            AssetDatabase.DeleteAsset(TestRoot);
        }

        [Test]
        public void Resolve_CreatesOnceAndReturnsCachedAsset()
        {
            var registration = Registration<SOSettingsLifecycleFixture>();

            ActionFitSettingsAssetResolution first =
                ActionFitSettingsAssetProvider.ResolveForTests(typeof(SOSettingsLifecycleFixture), registration, true);
            ActionFitSettingsAssetResolution second =
                ActionFitSettingsAssetProvider.Resolve(typeof(SOSettingsLifecycleFixture), true);

            Assert.That(first.Status, Is.EqualTo(ActionFitSettingsAssetStatus.Created));
            Assert.That(first.Asset, Is.Not.Null);
            Assert.That(((SOSettingsLifecycleFixture)first.Asset).Initialized, Is.True);
            Assert.That(second.Asset, Is.SameAs(first.Asset));
            Assert.That(second.IsCacheHit, Is.True);
        }

        [Test]
        public void Resolve_ReusesCanonicalLegacyAndUniqueAssets()
        {
            SOSettingsLifecycleFixture canonical = CreateAsset<SOSettingsLifecycleFixture>(
                $"{TestRoot}/{nameof(SOSettingsLifecycleFixture)}.asset");

            ActionFitSettingsAssetResolution canonicalResult =
                ActionFitSettingsAssetProvider.ResolveForTests(
                    typeof(SOSettingsLifecycleFixture),
                    Registration<SOSettingsLifecycleFixture>(),
                    true);
            Assert.That(canonicalResult.Status, Is.EqualTo(ActionFitSettingsAssetStatus.FoundCanonical));
            Assert.That(canonicalResult.Asset, Is.SameAs(canonical));

            ResetTestAssets();
            SOSettingsLifecycleFixture legacy = CreateAsset<SOSettingsLifecycleFixture>(
                $"{TestRoot}/Legacy.asset");
            var legacyRegistration = Registration<SOSettingsLifecycleFixture>();
            legacyRegistration.LegacyPaths = new[] { $"{TestRoot}/Legacy.asset" };
            ActionFitSettingsAssetResolution legacyResult =
                ActionFitSettingsAssetProvider.ResolveForTests(
                    typeof(SOSettingsLifecycleFixture), legacyRegistration, true);
            Assert.That(legacyResult.Status, Is.EqualTo(ActionFitSettingsAssetStatus.FoundLegacy));
            Assert.That(legacyResult.Asset, Is.SameAs(legacy));

            ResetTestAssets();
            SOSettingsLifecycleFixture unique = CreateAsset<SOSettingsLifecycleFixture>(
                $"{TestRoot}/Unique.asset");
            ActionFitSettingsAssetResolution uniqueResult =
                ActionFitSettingsAssetProvider.ResolveForTests(
                    typeof(SOSettingsLifecycleFixture),
                    Registration<SOSettingsLifecycleFixture>(),
                    true);

            Assert.That(uniqueResult.Status, Is.EqualTo(ActionFitSettingsAssetStatus.FoundUnique));
            Assert.That(uniqueResult.Asset, Is.SameAs(unique));
        }

        [Test]
        public void Resolve_BlocksDuplicatesAndOccupiedCanonicalPath()
        {
            CreateAsset<SOSettingsLifecycleFixture>($"{TestRoot}/DuplicateA.asset");
            CreateAsset<SOSettingsLifecycleFixture>($"{TestRoot}/DuplicateB.asset");

            ActionFitSettingsAssetResolution duplicate =
                ActionFitSettingsAssetProvider.ResolveForTests(
                    typeof(SOSettingsLifecycleFixture),
                    Registration<SOSettingsLifecycleFixture>(),
                    true);
            Assert.That(duplicate.Status, Is.EqualTo(ActionFitSettingsAssetStatus.Duplicate));
            Assert.That(duplicate.Asset, Is.Null);

            ResetTestAssets();
            CreateAsset<SOSettingsOtherFixture>(
                $"{TestRoot}/{nameof(SOSettingsLifecycleFixture)}.asset");
            ActionFitSettingsAssetResolution occupied =
                ActionFitSettingsAssetProvider.ResolveForTests(
                    typeof(SOSettingsLifecycleFixture),
                    Registration<SOSettingsLifecycleFixture>(),
                    true);

            Assert.That(occupied.Status, Is.EqualTo(ActionFitSettingsAssetStatus.OccupiedCanonicalPath));
            Assert.That(occupied.Asset, Is.Null);
        }

        [Test]
        public void Resolve_ReportsMissingAndCreationGuardWithoutMutation()
        {
            var registration = Registration<SOSettingsLifecycleFixture>();

            ActionFitSettingsAssetResolution missing =
                ActionFitSettingsAssetProvider.ResolveForTests(
                    typeof(SOSettingsLifecycleFixture), registration, false);
            ActionFitSettingsAssetProvider.CreationAllowedOverride = () => false;
            ActionFitSettingsAssetResolution blocked =
                ActionFitSettingsAssetProvider.ResolveForTests(
                    typeof(SOSettingsLifecycleFixture), registration, true);

            Assert.That(missing.Status, Is.EqualTo(ActionFitSettingsAssetStatus.Missing));
            Assert.That(blocked.Status, Is.EqualTo(ActionFitSettingsAssetStatus.CreationBlocked));
            Assert.That(AssetDatabase.LoadMainAssetAtPath(registration.CanonicalPath), Is.Null);
        }

        [Test]
        public void Resolve_ValidatesOwnerAndRuntimeSingletonBase()
        {
            var invalidOwner = new ActionFitSettingsAssetAttribute(
                "../Invalid",
                ActionFitSettingsAssetLifetime.EditorOnly);
            var invalidRuntime = Registration<SOSettingsLifecycleFixture>(
                ActionFitSettingsAssetLifetime.RuntimeSingleton);
            var validRuntime = Registration<SOSettingsRuntimeFixture>(
                ActionFitSettingsAssetLifetime.RuntimeSingleton);

            ActionFitSettingsAssetResolution ownerResult =
                ActionFitSettingsAssetProvider.ResolveForTests(
                    typeof(SOSettingsLifecycleFixture), invalidOwner, true);
            ActionFitSettingsAssetResolution invalidRuntimeResult =
                ActionFitSettingsAssetProvider.ResolveForTests(
                    typeof(SOSettingsLifecycleFixture), invalidRuntime, true);
            ActionFitSettingsAssetResolution validRuntimeResult =
                ActionFitSettingsAssetProvider.ResolveForTests(
                    typeof(SOSettingsRuntimeFixture), validRuntime, true);

            Assert.That(ownerResult.Status, Is.EqualTo(ActionFitSettingsAssetStatus.InvalidRegistration));
            Assert.That(invalidRuntimeResult.Status, Is.EqualTo(ActionFitSettingsAssetStatus.InvalidRuntimeBase));
            Assert.That(validRuntimeResult.Status, Is.EqualTo(ActionFitSettingsAssetStatus.Created));
            Assert.That(
                validRuntimeResult.CanonicalPath,
                Does.EndWith($"/Resources/SO/{nameof(SOSettingsRuntimeFixture)}.asset"));
        }

        [Test]
        public void Resolve_ClearCacheForcesCanonicalReload()
        {
            var registration = Registration<SOSettingsLifecycleFixture>();
            ActionFitSettingsAssetResolution created =
                ActionFitSettingsAssetProvider.ResolveForTests(
                    typeof(SOSettingsLifecycleFixture), registration, true);

            ActionFitSettingsAssetProvider.ClearCache();
            ActionFitSettingsAssetResolution reloaded =
                ActionFitSettingsAssetProvider.ResolveForTests(
                    typeof(SOSettingsLifecycleFixture), registration, false);

            Assert.That(created.Status, Is.EqualTo(ActionFitSettingsAssetStatus.Created));
            Assert.That(reloaded.Status, Is.EqualTo(ActionFitSettingsAssetStatus.FoundCanonical));
            Assert.That(reloaded.IsCacheHit, Is.False);
        }

        [Test]
        public void Audit_ReportsCanonicalAssetWithoutLoadingIt()
        {
            CreateAsset<SOSettingsLifecycleFixture>(
                $"{TestRoot}/{nameof(SOSettingsLifecycleFixture)}.asset");

            ActionFitSettingsAssetResolution result =
                ActionFitSettingsAssetProvider.AuditForTests(
                    typeof(SOSettingsLifecycleFixture),
                    Registration<SOSettingsLifecycleFixture>());

            Assert.That(result.Status, Is.EqualTo(ActionFitSettingsAssetStatus.FoundCanonical));
            Assert.That(result.Asset, Is.Null);
            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void Singleton_CachesPresentAndMissingLookupsUntilSubsystemReset()
        {
            SOSettingsRuntimeFixture instance = ScriptableObject.CreateInstance<SOSettingsRuntimeFixture>();
            int presentCalls = 0;
            SOSettingsRuntimeFixture.SetLoaderForTests(_ =>
            {
                presentCalls++;
                return instance;
            });

            Assert.That(SOSettingsRuntimeFixture.Instance, Is.SameAs(instance));
            Assert.That(SOSettingsRuntimeFixture.Instance, Is.SameAs(instance));
            Assert.That(presentCalls, Is.EqualTo(1));

            SO_SingletonRuntimeReset.ResetAllForTests();
            Assert.That(SOSettingsRuntimeFixture.Instance, Is.SameAs(instance));
            Assert.That(presentCalls, Is.EqualTo(2));

            int missingCalls = 0;
            SOSettingsRuntimeFixture.SetLoaderForTests(_ =>
            {
                missingCalls++;
                return null;
            });

            Assert.That(SOSettingsRuntimeFixture.Instance, Is.Null);
            Assert.That(SOSettingsRuntimeFixture.Instance, Is.Null);
            Assert.That(missingCalls, Is.EqualTo(1));
            Object.DestroyImmediate(instance);
        }

        private static ActionFitSettingsAssetAttribute Registration<T>(
            ActionFitSettingsAssetLifetime lifetime = ActionFitSettingsAssetLifetime.EditorOnly)
        {
            string suffix = lifetime == ActionFitSettingsAssetLifetime.RuntimeSingleton
                ? $"Resources/SO/{typeof(T).Name}.asset"
                : $"{typeof(T).Name}.asset";
            return new ActionFitSettingsAssetAttribute("MCC1541Tests", lifetime)
            {
                CanonicalPath = $"{TestRoot}/{suffix}"
            };
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            EnsureTestRoot();
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureTestRoot()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Data"))
            {
                AssetDatabase.CreateFolder("Assets", "_Data");
            }

            if (!AssetDatabase.IsValidFolder(TestRoot))
            {
                AssetDatabase.CreateFolder("Assets/_Data", "_MCC1541Tests");
            }
        }

        private static void ResetTestAssets()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            ActionFitSettingsAssetProvider.ResetForTests();
            ActionFitSettingsAssetProvider.CreationAllowedOverride = () => true;
        }
    }
}
