using Assets.Scripts.Objects;

namespace ImageScreens.Prefabs
{
    // Base class for save data of image screens.
    //
    // Screens of all shapes share the same save data structure.
    // The kind of screen is discriminated by the ThingSaveData.PrefabName.
    public class ImageScreenSaveData : StructureSaveData
    {
        // Specification what the screen is showing.
        public ImageScreenSpec Spec;
    }
}
