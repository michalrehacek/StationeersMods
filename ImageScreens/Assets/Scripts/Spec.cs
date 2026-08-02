namespace ImageScreens.Prefabs
{
    // Parameters defining what the user wants to be shown on a screen and how.
    public class ImageScreenSpec
    {
        // URL to download from.
        //
        // This can be either an image or an index text file.
        public string URL;

        // Does the specified screen specification refer to a text file?
        public bool IsTextSpec()
        {
            return Utils.IsTextURL(URL);
        }

        // Does the specified screen specification refer to an image file?
        //
        // A non-empty URL that isn't Text URL is considered to be an image by default.
        public bool IsImageSpec()
        {
            return Utils.IsImageURL(URL);
        }
    }
}
