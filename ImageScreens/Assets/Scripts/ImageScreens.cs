using HarmonyLib;
using ImageScreens.Prefabs;
using LaunchPadBooster;
using LaunchPadBooster.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace ImageScreens
{
    // Main mod class for Image screens.
    public class ImageScreens : MonoBehaviour
    {
        public static readonly Mod MOD = new(Version.ModName, Version.ModVersion);

        // Instance of the ImageScreens mod object.
        public static ImageScreens Instance;
        // Harmony patcher instance.
        private Harmony Harmony;

        // Texture used for empty screens.
        public Texture2D EmptyTexture;
        // Texture used in case of an error.
        public Texture2D ErrorTexture;

        // Texture cache, dealing with downloads from the web.
        public DownloadCache Cache;

        // Find the asset bundle of this mod.
        private AssetBundle GetMyAssetBundle()
        {
            // We can't get hold of it as a parameter in one of the callback,
            // so we will just enumerate all loaded bundles.
            foreach (var bundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (bundle.name == "imagescreens.assets")
                {
                    return bundle;
                }
            }
            return null;
        }

        // Callback when the object is waking up.
        public void Awake()
        {
            Log.Info("ImageScreens::Awake");

            // Remember we are the class instance.
            Instance = this;
            Cache = new DownloadCache(this);

            // Patch Stationeers methods.
            Harmony = new Harmony(Version.ModID);
            Harmony.PatchAll();
        }

        // Callback when destroyed.
        public void OnDestroy()
        {
            Log.Info("ImageScreens::OnDestroy");

            // Unpatch Stationeers data.
            Harmony.UnpatchSelf();
            Harmony = null;

            // Destroy the texture cache.
            Cache = null;

            // Instance gone.
            Instance = null;
        }

        // Main callback to initialize the mod.
        public void OnLoaded(List<GameObject> prefabs)
        {
            Log.Info("ImageScreens::OnLoaded");

            // Get the sprite for the default and error images.
            var defaultSprite = Shader.Find("Sprites/Default");
            var bundle = GetMyAssetBundle();
            var EmptySprite = bundle.LoadAsset<Sprite>("EmptyScreen");
            EmptyTexture = EmptySprite.texture;
            var ErrorSprite = bundle.LoadAsset<Sprite>("ErrorScreen");
            ErrorTexture = ErrorSprite.texture;

            // Initialize all prefabs this mod is adding.
            MOD.AddPrefabs(prefabs);

            // Initialize save data types.
            MOD.AddSaveDataType<ImageScreenSaveData>();

            // Prefabs.
            MOD.SetupPrefabs<ImageScreenLandscape>()
                .AddToMultiConstructorKit("ItemKitSign")
                .SetEntryTool("ItemKitSign")
                .SetExitTool(PrefabNames.Drill)
                .RunFunc(prefab =>
                {
                    prefab.BuildStates[0].Tool.EntryQuantity = 1;
                    prefab.BuildStates[0].Tool.EntryTime = 1.0f;
                    prefab.BuildStates[0].Tool.ExitTime = 1.0f;
                    prefab.BuildStates[0].Thumbnail = EmptySprite;
                    prefab.Thumbnail = EmptySprite;
                });

            MOD.SetupPrefabs<ImageScreenPortrait>()
                .AddToMultiConstructorKit("ItemKitSign")
                .SetEntryTool("ItemKitSign")
                .SetExitTool(PrefabNames.Drill)
                .RunFunc(prefab =>
                {
                    prefab.BuildStates[0].Tool.EntryQuantity = 1;
                    prefab.BuildStates[0].Tool.EntryTime = 1.0f;
                    prefab.BuildStates[0].Tool.ExitTime = 1.0f;
                    prefab.BuildStates[0].Thumbnail = EmptySprite;
                    prefab.Thumbnail = EmptySprite;
                });

            Log.Info($"Loaded {prefabs.Count} prefabs");
        }

        // Callback when the world is cleared.
        public void OnClearWorld()
        {
            Log.Info("ImageScreens::OnClearWorld");
            Cache.Clear();
        }
    }
}
