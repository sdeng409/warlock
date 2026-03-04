namespace Warlock.Mvp
{
    public static class MvpVersionLock
    {
        public const string UnityVersion = "6000.0.68f1";
        public const string RenderPipeline = "URP";
        public const string FusionVersion = "2.0.11 Stable";
        public const string FusionBuild = "1743";
        public const string InputSystemVersion = "1.17.0";

        public static bool IsLocked(string unityVersion, string fusionVersion, string fusionBuild, string inputSystemVersion)
        {
            return unityVersion == UnityVersion
                   && fusionVersion == FusionVersion
                   && fusionBuild == FusionBuild
                   && inputSystemVersion == InputSystemVersion;
        }
    }
}
