using ActionFit.SOSingleton;
using UnityEngine;

namespace ActionFit.SOSingleton.Editor.Tests
{
    internal sealed class SOSettingsLifecycleFixture :
        ScriptableObject,
        IActionFitSettingsAssetInitializer
    {
        public bool Initialized { get; private set; }

        public void InitializeNewSettingsAsset()
        {
            Initialized = true;
        }
    }
}
