using Assets.Scripts;
using UnityEngine;

namespace ImageScreens
{
    // Game strings this mod uses.
    //
    // I was not able to make localization work with GameString.
    // Localization is loaded fairly late, after the mod initializes, and definitely after the global init.
    // The strings cannot be constants, they must be properties to force the GetInterface call when accessed.
    internal static class ModStrings
    {
        internal static readonly int ErrorHash = Animator.StringToHash("ImageScreenError");
        internal static readonly int FailedToDownloadHash = Animator.StringToHash("ImageScreenFailedToDownload");
        public static string Error => Localization.GetInterface(ErrorHash);
        public static string FailedToDownload => Localization.GetInterface(FailedToDownloadHash);
    }
}
