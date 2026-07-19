using System;

namespace ActionFit.SOSingleton
{
    public enum ActionFitSettingsAssetLifetime
    {
        EditorOnly,
        RuntimeSingleton
    }

    /// <summary>패키지 설정 SO의 소유자, 생명주기와 탐색 경로 계약을 선언합니다.</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ActionFitSettingsAssetAttribute : Attribute
    {
        public ActionFitSettingsAssetAttribute(
            string owner,
            ActionFitSettingsAssetLifetime lifetime)
        {
            Owner = owner;
            Lifetime = lifetime;
        }

        public string Owner { get; }
        public ActionFitSettingsAssetLifetime Lifetime { get; }
        public string CanonicalPath { get; set; }
        public string[] LegacyPaths { get; set; } = Array.Empty<string>();
        public bool AutoCreate { get; set; } = true;
    }

    /// <summary>새 설정 에셋이 저장되기 전에 패키지 기본값을 적용합니다.</summary>
    public interface IActionFitSettingsAssetInitializer
    {
        void InitializeNewSettingsAsset();
    }
}
